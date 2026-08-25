using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Resources;
using System.Resources.NetStandard;
using System.Text;
using System.Xml.Linq;
using Installer.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Installer;

internal class Compiler
{
	private ZipArchive ProjectArchive { get; }
	private Installation Installation { get; }
	private XDocument ProjectDocument { get; }
	private string ProjectDirectory { get; }

	private string[] Exclude { get; }
	private string[] Defines { get; }
	private IReadOnlyDictionary<string, MetadataReference> ProjectReferences { get; }

	private static CSharpCompilationOptions CompilationOptions { get; } =
		new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
			.WithOverflowChecks(true)
			.WithOptimizationLevel(OptimizationLevel.Release)
			.WithDeterministic(true);

	public Compiler(ZipArchive projectArchive, CompilationContext context)
	{
		ProjectArchive = projectArchive;
		Installation = context.Installation;
		Exclude = context.Exclude;
		Defines = context.Defines;
		ProjectReferences = context.ProjectReferences;

		var entry = FindArchiveEntry(context.Project);
		var separator = entry.FullName.LastIndexOf(Path.AltDirectorySeparatorChar);
		ProjectDirectory = separator < 0 ? string.Empty : entry.FullName.Substring(0, separator + 1);
		using var stream = entry.Open();
		using var reader = new StreamReader(stream);
		ProjectDocument = XDocument.Parse(reader.ReadToEnd(), LoadOptions.None);
	}

	private ZipArchiveEntry FindArchiveEntry(string relativePath)
	{
		var normalized = relativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).TrimStart(Path.AltDirectorySeparatorChar);
		var suffix = Path.AltDirectorySeparatorChar + normalized;
		var matches = ProjectArchive.Entries
			.Where(entry => entry.Name.Length > 0 && (entry.FullName.Equals(normalized, StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
			.Take(2)
			.ToArray();

		return matches.Length switch
		{
			1 => matches[0],
			0 => throw new FileNotFoundException($"Project file {relativePath} was not found in the repository snapshot"),
			_ => throw new InvalidDataException($"Project file {relativePath} is ambiguous in the repository snapshot")
		};
	}

	private ZipArchiveEntry GetProjectEntry(string relativePath)
	{
		var expected = NormalizeArchivePath(ProjectDirectory + relativePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		var matches = ProjectArchive.Entries
			.Where(entry => entry.Name.Length > 0 && entry.FullName.Equals(expected, StringComparison.OrdinalIgnoreCase))
			.Take(2)
			.ToArray();

		return matches.Length switch
		{
			1 => matches[0],
			0 => throw new FileNotFoundException($"Project file {relativePath} was not found in the repository snapshot"),
			_ => throw new InvalidDataException($"Project file {relativePath} is ambiguous in the repository snapshot")
		};
	}

	private static string NormalizeArchivePath(string path)
	{
		var segments = new List<string>();
		foreach (var segment in path.Split([Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
		{
			if (segment == ".")
				continue;

			if (segment == "..")
			{
				if (segments.Count == 0)
					throw new InvalidDataException("Project path escapes the repository snapshot");

				segments.RemoveAt(segments.Count - 1);
				continue;
			}

			segments.Add(segment);
		}

		return string.Join(Path.AltDirectorySeparatorChar, segments);
	}

	private IEnumerable<string> GetSourceFiles()
	{
		foreach (var file in GetIncludes("Compile"))
		{
			if (!Exclude.Contains(file, StringComparer.OrdinalIgnoreCase))
				yield return file;
			else
			{
#if DEBUG
				Spectre.Console.AnsiConsole.MarkupLine($"[grey]>> Excluding {Spectre.Console.StringExtensions.EscapeMarkup(file)}.[/]");
#endif
			}
		}
	}

	private IEnumerable<string> GetIncludes(string elementName)
	{
		return ProjectDocument
			.Descendants()
			.Where(element => element.Name.LocalName == elementName)
			.Select(element => element.Attribute("Include")?.Value)
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.OfType<string>();
	}

	private bool TryGetMetadataReference(string assemblyName, [NotNullWhen(true)] out MetadataReference? reference)
	{
		if (ProjectReferences.TryGetValue(assemblyName, out reference))
			return true;

		if (TryGetAssemblyPath(assemblyName, out var path))
		{
			reference = MetadataReference.CreateFromFile(path);
#if DEBUG
			Spectre.Console.AnsiConsole.MarkupLine($"[grey]>> Resolved {assemblyName} to {Spectre.Console.StringExtensions.EscapeMarkup(path)}.[/]");
#endif
		}

		if (reference == null && TryGetAssemblyBytes(assemblyName, out var stream))
		{
#if DEBUG
			Spectre.Console.AnsiConsole.MarkupLine($"[grey]>> Using memory image for {assemblyName}.[/]");
#endif
			reference = MetadataReference.CreateFromImage(stream);
		}

		if (reference == null)
		{
#if DEBUG
			Spectre.Console.AnsiConsole.MarkupLine($"[grey]>> Unable to resolve {assemblyName}.[/]");
#endif
		}

		return reference != null;
	}

	private bool TryGetAssemblyPath(string assemblyName, out string path)
	{
		path = Path.Combine(Installation.Managed, $"{assemblyName}.dll");
		if (!File.Exists(path))
			path = Path.Combine(Installation.BepInExCore, $"{assemblyName}.dll");

		return File.Exists(path);
	}

	private static bool TryGetAssemblyBytes(string assemblyName, [NotNullWhen(true)] out byte[]? buffer)
	{
		try
		{
			buffer = Resources.ResourceManager.GetObject(assemblyName) as byte[];
		}
		catch
		{
			buffer = null;
		}
		return buffer != null;
	}

	private IEnumerable<MetadataReference> GetReferences()
	{
		yield return MetadataReference.CreateFromFile(Path.Combine(Installation.Managed, "mscorlib.dll"));

		foreach (var include in GetIncludes("Reference").Concat(GetIncludes("ProjectReference")))
		{
			var assemblyName = Path.GetFileName(include.Split(',')[0]);
			if (assemblyName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				assemblyName = Path.GetFileNameWithoutExtension(assemblyName);

			if (TryGetMetadataReference(assemblyName, out var reference))
				yield return reference;
		}
	}

	private IEnumerable<SyntaxTree> GetSyntaxTrees()
	{
		var options = CSharpParseOptions
			.Default
			.WithLanguageVersion(LanguageVersion.Latest)
			.WithPreprocessorSymbols(Defines);

		foreach (var file in GetSourceFiles())
		{
			var entry = GetProjectEntry(file);

			using var stream = entry.Open();
			using var reader = new StreamReader(stream);

			var text = reader.ReadToEnd();
			var sourceText = SourceText.From(text, Encoding.UTF8);
			yield return SyntaxFactory.ParseSyntaxTree(sourceText, options, file);
		}
	}

	public bool IsLocalizationSupported()
	{
		return IsLanguageSupported(null);
	}

	public bool IsLanguageSupported(CompilationContext? context)
	{
		return GetSourceFiles().Any(f => f.EndsWith(string.Concat("Strings.", context?.Language ?? string.Empty, ".Designer.cs").Replace("..", "."), StringComparison.OrdinalIgnoreCase));
	}

	public IEnumerable<ResourceDescription> GetResources(CompilationContext context)
	{
		foreach (var file in GetIncludes("EmbeddedResource"))
		{
			if (!file.EndsWith(string.Concat("Strings.", context.Language, ".resx").Replace("..", "."), StringComparison.OrdinalIgnoreCase))
				continue;

			var entry = GetProjectEntry(file);

			using var stream = entry.Open();
			using var reader = new ResXResourceReader(stream);

			using var memory = new MemoryStream();
			using var writer = new ResourceWriter(memory);

			foreach (DictionaryEntry resourcEntry in reader)
				writer.AddResource(resourcEntry.Key.ToString()!, resourcEntry.Value);

			writer.Generate();
			var resource = memory.ToArray();

			var resourceName = "RavenX.Properties." + Path.GetFileName(file)
				.Replace($".{context.Language}.", ".", StringComparison.OrdinalIgnoreCase)
				.Replace(".resx", ".resources");

			yield return new ResourceDescription(resourceName, () => new MemoryStream(resource, false), isPublic: true);
		}
	}

	public CSharpCompilation Compile(string assemblyName)
	{
		var syntaxTrees = GetSyntaxTrees()
			.ToArray();

		var references = GetReferences()
			.ToArray();

		return CSharpCompilation.Create(assemblyName, syntaxTrees, references, CompilationOptions);
	}
}
