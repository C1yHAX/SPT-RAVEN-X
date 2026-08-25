using System.IO.Compression;
using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Installer;

internal class CompilationResult(CSharpCompilation? compilation, ZipArchive? archive, Diagnostic[] errors, ResourceDescription[] resources, string branch)
{
	public CSharpCompilation? Compilation { get; } = compilation;
	public ZipArchive? Archive { get; } = archive;
	public Diagnostic[] Errors { get; } = errors;
	public ResourceDescription[] Resources { get; } = resources;
	public string Branch { get; } = branch;

	public string[] ErrorFiles
	{
		get
		{
			return [.. Errors
				.Select(d => d.Location.SourceTree?.FilePath)
				.Where(s => s is not null)
				.OfType<string>()
				.Select(path => path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)];
		}
	}
}
