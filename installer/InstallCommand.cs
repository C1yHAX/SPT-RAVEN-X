using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Installer.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Installer;

internal sealed class InstallCommand : AsyncCommand<InstallCommand.Settings>
{
	private const long MaximumSnapshotSize = 64L * 1024L * 1024L;
	private const long MaximumExpandedSnapshotSize = 128L * 1024L * 1024L;
	private const long MaximumSnapshotEntrySize = 32L * 1024L * 1024L;
	private const int MaximumSnapshotEntries = 5000;
	private static readonly HttpClient SnapshotClient = CreateSnapshotClient();
	private static readonly HashSet<string> RequiredSources = new(StringComparer.OrdinalIgnoreCase)
	{
		"Feature",
		"FeatureFactory",
		"ToggleFeature",
		"TriggerFeature",
		"HoldFeature",
		"CachableFeature",
		"PointOfInterest",
		"PointOfInterests",
		"Commands",
		"ConsoleCommand",
		"ConsoleCommandWithArgument",
		"ConsoleCommandWithoutArgument"
	};

	internal class Settings : CommandSettings
	{
		[Description("Path to EFT.")]
		[CommandArgument(0, "[path]")]
		public string? Path { get; set; }

		[Description("Use specific RavenX branch version.")]
		[CommandOption("-b|--branch")]
		public string? Branch { get; set; }

		[Description("Disable feature.")]
		[CommandOption("-f|--feature")]
		public string[]? DisabledFeatures { get; set; }

		[Description("Disable command.")]
		[CommandOption("-c|--command")]
		public string[]? DisabledCommands { get; set; }

		[Description("Language.")]
		[CommandOption("-l|--language")]
		public string Language { get; set; } = "";
	}

	public static string[] ToSourceFile(string[]? names, string folder)
	{
		names ??= [];
		return [.. names.Select(f => $"{folder}\\{f}.cs")];
	}

	[SupportedOSPlatform("windows")]
	public override async Task<int> ExecuteAsync(CommandContext commandContext, Settings settings, CancellationToken ct)
	{
		try
		{
			AnsiConsole.MarkupLine("-=[[ [cyan]SPT RAVEN-X Universal Installer[/] - [blue]https://github.com/C1yHAX/SPT-RAVEN-X [/]]]=-");
			AnsiConsole.WriteLine();

			var installation = Installation.GetTargetInstallation(settings.Path, "Please select where to install RavenX");
			if (installation == null)
				return (int)ExitCode.NoInstallationFound;

			AnsiConsole.MarkupLine($"Target [green]EscapeFromTarkov ({installation.Version})[/] in [blue]{installation.Location.EscapeMarkup()}[/].");

			if (installation.UsingSpt)
			{
				AnsiConsole.MarkupLine("[green][[SPT]][/] detected. Please make sure you have run the game at least once before installing RavenX.");
				AnsiConsole.MarkupLine("SPT is patching binaries during the first run, and we [underline]need[/] to compile against those patched binaries.");
				AnsiConsole.MarkupLine("If you install RavenX on stock binaries, we'll be unable to compile or the game will freeze at the startup screen.");

				if (installation.UsingSptButNeverRun)
					AnsiConsole.MarkupLine("[yellow]Warning: it seems that you have never run your SPT installation. You should quit now and rerun this installer once it's done.[/]");

				if (!await AnsiConsole.ConfirmAsync("Continue installation (yes I have run the game at least once) ?", cancellationToken: ct))
					return (int)ExitCode.Canceled;
			}

			const string features = "src\\Features";
			const string commands = "src\\ConsoleCommands";

			settings.DisabledFeatures = ToSourceFile(settings.DisabledFeatures, features);
			settings.DisabledCommands = ToSourceFile(settings.DisabledCommands, commands);

			var result = await BuildRavenXAsync(settings, installation, ct, features, commands);
			using var archive = result.Archive;

			if (result.Compilation == null || archive == null)
			{

				AnsiConsole.MarkupLine($"[red]Unable to compile RavenX for version {installation.Version}. Please file an issue here : https://github.com/C1yHAX/SPT-RAVEN-X/issues [/]");
				return (int)ExitCode.CompilationFailed;
			}

			const string bepInExPluginProject = "BepInExPlugin.csproj";
			CSharpCompilation? pluginCompilation = null;
			if (installation.UsingBepInEx && archive.Entries.Any(e => e.Name == bepInExPluginProject))
			{
				AnsiConsole.MarkupLine("[green][[BepInEx]][/] detected. Creating plugin instead of using NLog configuration.");

				var pluginContext = new CompilationContext(installation, "plugin", bepInExPluginProject)
				{
					Archive = archive,
					Branch = result.Branch
				};

				if (result.Compilation.AssemblyName is { Length: > 0 } projectAssemblyName)
					pluginContext.ProjectReferences[projectAssemblyName] = result.Compilation.ToMetadataReference();

				var pluginResult = await GetCompilationAsync(pluginContext, ct);

				if (pluginResult.Compilation == null)
				{
					AnsiConsole.MarkupLine($"[red]Unable to compile plugin for version {installation.Version}. Please file an issue here : https://github.com/C1yHAX/SPT-RAVEN-X/issues [/]");
					return (int)ExitCode.PluginCompilationFailed;
				}

				pluginCompilation = pluginResult.Compilation;
			}
			else
			{
				var version = new Version(0, 13, 0, 21531);
				if (installation.Version >= version)
				{
					AnsiConsole.MarkupLine($"[yellow]Warning: EscapeFromTarkov {version} or later prevent RavenX from being loaded using NLog configuration.[/]");
					AnsiConsole.MarkupLine("[yellow]It is now mandatory to use SPT/BepInEx, or to find your own way to load RavenX. As is, it will not work.[/]");
				}

			}

			using var transaction = new FileTransaction();

			if (!CreateDll(transaction, installation, "RavenX.dll", dllPath => result.Compilation.Emit(dllPath, manifestResources: result.Resources)))
				return (int)ExitCode.CreateDllFailed;

			if (!CreateDll(transaction, installation, "0Harmony.dll", dllPath => File.WriteAllBytes(dllPath, Resources._0Harmony), false))
				return (int)ExitCode.CreateHarmonyDllFailed;

			if (!CreateOutline(transaction, installation, archive))
				return (int)ExitCode.CreateOutlineFailed;

			if (pluginCompilation != null)
			{
				if (!CreateDll(transaction, installation, Path.Combine(installation.BepInExPlugins, "RavenX.Plugin.dll"), dllPath => pluginCompilation.Emit(dllPath)))
					return (int)ExitCode.CreatePluginDllFailed;
			}
			else if (!CreateOrPatchConfiguration(transaction, installation))
			{
				return (int)ExitCode.CreateConfigurationFailed;
			}

			transaction.Commit();

			TryCreateGameDocumentFolder();
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return (int)ExitCode.Canceled;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}. Please file an issue here : https://github.com/C1yHAX/SPT-RAVEN-X/issues [/]");
			return (int)ExitCode.Failure;
		}

		return (int)ExitCode.Success;
	}

	private static async Task<CompilationResult> BuildRavenXAsync(Settings settings, Installation installation, CancellationToken ct, params string[] folders)
	{
		var context = new CompilationContext(installation, "ravenx", "RavenX.csproj")
		{
			Exclude = [.. settings.DisabledFeatures!, .. settings.DisabledCommands!],
			Branch = GetInitialBranch(settings),
			Defines = installation.UsingSpt ? [] : ["EFT_LIVE"],
			Language = settings.Language
		};

		var result = await GetCompilationAsync(context, ct);
		if (context.IsFatalFailure || result.Compilation != null)
			return result;

		var selectedBranch = context.Branch;
		var retryBranch = GetRetryBranch(installation, context);
		if (retryBranch != null)
		{
			context.Branch = retryBranch;
			var retry = await GetCompilationAsync(context, ct);

			if (retry.Compilation != null)
			{
				DisposeArchive(result);
				return retry;
			}

			if (context.IsFatalFailure)
			{
				if (!HasFaultingFeatureOrCommand(result, folders, result.ErrorFiles))
				{
					DisposeArchive(result);
					return retry;
				}

				context.IsFatalFailure = false;
				context.Branch = selectedBranch;
				DisposeArchive(retry);
			}

			else if (HasFaultingFeatureOrCommand(retry, folders, retry.ErrorFiles))
			{
				DisposeArchive(result);
				result = retry;
				selectedBranch = retryBranch;
			}
			else if (result.Errors.Length == 0 && retry.Errors.Length > 0)
			{
				DisposeArchive(result);
				result = retry;
				selectedBranch = retryBranch;
			}
			else
			{
				DisposeArchive(retry);
				context.Branch = selectedBranch;
			}
		}

		var files = result.ErrorFiles;
		if (!HasFaultingFeatureOrCommand(result, folders, files))
			return result;

		var names = GetFaultingNames(files);
		if (names.Length == 0)
			return result;

		AnsiConsole.MarkupLine($"[yellow]Trying to disable faulting feature/command: [red]{names.EscapeMarkup()}[/].[/]");

		context.Exclude = [.. files
			.Concat(settings.DisabledFeatures!)
			.Concat(settings.DisabledCommands!)
			.Distinct(StringComparer.OrdinalIgnoreCase)];
		context.Branch = selectedBranch;

		DisposeArchive(result);
		result = await GetCompilationAsync(context, ct);

		if (result.Compilation != null)
			AnsiConsole.MarkupLine("[yellow]We found a fallback! But please file an issue here : https://github.com/C1yHAX/SPT-RAVEN-X/issues [/]");

		return result;
	}

	private static bool HasFaultingFeatureOrCommand(CompilationResult result, string[] folders, string[] files)
	{
		return result.Compilation == null && files.Length != 0 && files.All(file => IsOptionalSource(file, folders));
	}

	private static string GetFaultingNames(string[] files)
	{
		return string.Join(", ", files
			.Select(Path.GetFileNameWithoutExtension)
			.Where(name => !string.IsNullOrEmpty(name))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
	}

	private static bool IsOptionalSource(string file, string[] folders)
	{
		var normalized = file.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		if (!folders.Any(folder => normalized.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
			return false;

		var name = Path.GetFileNameWithoutExtension(normalized);
		if (string.IsNullOrEmpty(name) || name.StartsWith("Base", StringComparison.OrdinalIgnoreCase))
			return false;

		return !RequiredSources.Contains(name);
	}

	private static void DisposeArchive(CompilationResult result)
	{
		result.Archive?.Dispose();
	}

	private static string GetDefaultBranch()
	{
		return CompilationContext.DefaultBranch;
	}

	private static string GetInitialBranch(Settings settings)
	{
		var branch = string.IsNullOrWhiteSpace(settings.Branch) ? GetDefaultBranch() : settings.Branch.Trim();
		if (!IsValidBranch(branch))
			throw new ArgumentException($"Invalid branch name: {branch}");

		return branch;
	}

	private static string? GetRetryBranch(Installation installation, CompilationContext context)
	{
		var dedicated = "dev-" + installation.Version;
		return string.Equals(dedicated, context.Branch, StringComparison.OrdinalIgnoreCase) ? null : dedicated;
	}

	private static bool IsValidBranch(string branch)
	{
		if (branch.Length is 0 or > 200 || branch == "@" || branch.Contains("..", StringComparison.Ordinal) || branch.Contains("//", StringComparison.Ordinal))
			return false;

		if (branch[0] is '-' or '/' or '.' || branch[^1] is '/' or '.')
			return false;

		foreach (var segment in branch.Split('/'))
		{
			if (segment.Length == 0 || segment[0] == '.' || segment.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
				return false;
		}

		return branch.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or '/');
	}

	private static void TryCreateGameDocumentFolder()
	{
		var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Escape from Tarkov");
		if (Directory.Exists(folder))
			return;

		try
		{
			Directory.CreateDirectory(folder);
			AnsiConsole.MarkupLine($"Created [blue]{folder.EscapeMarkup()}[/] folder.");
		}
		catch (Exception)
		{
			AnsiConsole.MarkupLine($"[yellow]Unable to create [blue]{folder.EscapeMarkup()}[/]. We need this folder to store our [green]ravenx.ini[/] later.[/]");
		}
	}

	private static async Task<CompilationResult> GetCompilationAsync(CompilationContext context, CancellationToken ct)
	{
		var errors = Array.Empty<Diagnostic>();
		ResourceDescription[] resources = [];

		var archive = context.Archive ?? await GetSnapshotAsync(context, context.Branch, ct);
		if (archive == null)
		{
			context.Try++;
			return new(null, null, errors, resources, context.Branch);
		}

		CSharpCompilation? compilation = null;

		ct.ThrowIfCancellationRequested();
		AnsiConsole
			.Status()
			.Start($"Compiling {context.ProjectTitle}", _ =>
			{
				var compiler = new Compiler(archive, context);
				compilation = compiler.Compile(Path.GetFileNameWithoutExtension(context.Project));
				errors = [.. compilation
					.GetDiagnostics()
					.Where(d => d.Severity == DiagnosticSeverity.Error)];

				foreach (var error in errors.Take(20))
					AnsiConsole.MarkupLine($"[grey]>> {error.Id} [[{error.Location.SourceTree?.FilePath.EscapeMarkup()}]]: {error.GetMessage().EscapeMarkup()}.[/]");

				if (errors.Length > 20)
					AnsiConsole.MarkupLine($"[grey]>> {errors.Length - 20} additional compiler error(s) omitted.[/]");

				if (errors.Length != 0)
				{
					AnsiConsole.MarkupLine($">> [blue]Try #{context.Try}[/] [yellow]Compilation failed for {context.Branch.EscapeMarkup()} branch.[/]");
					compilation = null;
				}
				else
				{
					resources = [.. compiler.GetResources(context)];

					if (compiler.IsLocalizationSupported() && resources.Length == 0)
					{
						AnsiConsole.MarkupLine($"[yellow]Warning: no localization support for language '{context.Language.EscapeMarkup()}'.[/]");
						compilation = null;
						context.IsFatalFailure = true;
					}
					else
					{
						AnsiConsole.MarkupLine($">> [blue]Try #{context.Try}[/] Compilation [green]succeed[/] for [blue]{context.Branch.EscapeMarkup()}[/] branch.");
					}
				}
			});

		ct.ThrowIfCancellationRequested();
		context.Try++;
		return new(compilation, archive, errors, resources, context.Branch);
	}

	private static async Task<ZipArchive?> GetSnapshotAsync(CompilationContext context, string branch, CancellationToken ct)
	{
		var status = $"Downloading repository snapshot ({branch} branch)...";
		ZipArchive? result = null;

		try
		{
			await AnsiConsole
				.Status()
				.StartAsync(status, async _ =>
				{
					ct.ThrowIfCancellationRequested();
					var escapedBranch = Uri.EscapeDataString(branch);
					var uri = new Uri($"https://codeload.github.com/C1yHAX/SPT-RAVEN-X/zip/refs/heads/{escapedBranch}");
					using var request = new HttpRequestMessage(HttpMethod.Get, uri);
					using var response = await SnapshotClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
					response.EnsureSuccessStatusCode();

					var declaredLength = response.Content.Headers.ContentLength;
					if (declaredLength > MaximumSnapshotSize)
						throw new InvalidDataException("Repository snapshot is too large");

					var capacity = declaredLength is > 0 and <= int.MaxValue ? (int)declaredLength.Value : 0;
					var stream = new MemoryStream(capacity);
					try
					{
						await using var input = await response.Content.ReadAsStreamAsync(ct);
						var buffer = new byte[81920];
						long total = 0;

						while (true)
						{
							var read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
							if (read == 0)
								break;

							total += read;
							if (total > MaximumSnapshotSize)
								throw new InvalidDataException("Repository snapshot is too large");

							await stream.WriteAsync(buffer.AsMemory(0, read), ct);
						}

						stream.Position = 0;
						var archive = new ZipArchive(stream, ZipArchiveMode.Read);
						try
						{
							ValidateSnapshot(archive);
							result = archive;
						}
						catch
						{
							archive.Dispose();
							throw;
						}
					}
					catch
					{
						await stream.DisposeAsync();
						throw;
					}
				});
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (InvalidDataException ex)
		{
			context.IsFatalFailure = true;
			AnsiConsole.MarkupLine($"[red]Error: {ex.Message.EscapeMarkup()}[/]");
		}
		catch (HttpRequestException ex) when (IsTlsFailure(ex))
		{
			context.IsFatalFailure = true;
			AnsiConsole.MarkupLine("[red]Error: The HTTPS certificate check failed. The repository snapshot was rejected.[/]");
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine(ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound } ? $">> [blue]Try #{context.Try}[/] [yellow]Branch {branch.EscapeMarkup()} not found.[/]" : $"[red]Error: {ex.Message.EscapeMarkup()}[/]");
		}

		return result;
	}

	private static bool IsTlsFailure(Exception exception)
	{
		for (Exception? current = exception; current != null; current = current.InnerException)
		{
			if (current is AuthenticationException)
				return true;
		}

		return false;
	}

	private static void ValidateSnapshot(ZipArchive archive)
	{
		if (archive.Entries.Count == 0 || archive.Entries.Count > MaximumSnapshotEntries)
			throw new InvalidDataException("Repository snapshot has an invalid entry count");

		long expandedSize = 0;
		foreach (var entry in archive.Entries)
		{
			if (entry.Length > MaximumSnapshotEntrySize)
				throw new InvalidDataException("Repository snapshot contains an oversized entry");

			expandedSize += entry.Length;
			if (expandedSize > MaximumExpandedSnapshotSize)
				throw new InvalidDataException("Repository snapshot expands beyond the allowed size");
		}
	}

	private static HttpClient CreateSnapshotClient()
	{
		var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
		client.DefaultRequestHeaders.UserAgent.ParseAdd("RavenX-Installer");
		return client;
	}

	private static bool CreateOrPatchConfiguration(FileTransaction transaction, Installation installation)
	{
		const string targetName = "EFTTarget";
		var configPath = Path.Combine(installation.Managed, "NLog.dll.nlog");
		var markerPath = configPath + ".ravenx-created";
		try
		{
			if (File.Exists(configPath))
			{
				var doc = LoadXml(configPath);

				var nlogNode = doc.DocumentElement;
				var targetsNode = nlogNode?.ChildNodes.Cast<XmlNode>().FirstOrDefault(node => node.NodeType == XmlNodeType.Element && node.LocalName == "targets");

				if (nlogNode?.LocalName != "nlog" || targetsNode == null)
				{
					AnsiConsole.MarkupLine($"[red]Unable to patch {configPath.EscapeMarkup()}, unexpected xml structure.[/]");
					return false;
				}

				if (targetsNode.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().Any(IsLoaderTarget))
				{
					AnsiConsole.MarkupLine($"Already patched [green]{Path.GetFileName(configPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(configPath).EscapeMarkup()}[/].");
					return true;
				}

				var entry = doc.CreateElement("target", targetsNode.NamespaceURI);
				entry.SetAttribute("name", targetName);
				entry.SetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance", targetName);
				targetsNode.AppendChild(entry);

				transaction.Track(configPath);
				AtomicFile.WriteText(configPath, SerializeXml(doc));

				AnsiConsole.MarkupLine($"Patched [green]{Path.GetFileName(configPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(configPath).EscapeMarkup()}[/].");
				return true;
			}

			var content = $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<nlog xmlns=""http://www.nlog-project.org/schemas/NLog.xsd"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <targets>
    <target name=""{targetName}"" xsi:type=""{targetName}"" />
  </targets>
</nlog>";
			transaction.Track(configPath);
			transaction.Track(markerPath);
			AtomicFile.WriteText(configPath, content);
			AtomicFile.WriteText(markerPath, "RavenX");
			AnsiConsole.MarkupLine($"Created [green]{Path.GetFileName(configPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(configPath).EscapeMarkup()}[/].");
			return true;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Unable to patch or create {configPath.EscapeMarkup()}: {ex.Message.EscapeMarkup()}.[/]");
			return false;
		}
	}

	private static XmlDocument LoadXml(string filename)
	{
		var settings = new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null
		};
		var document = new XmlDocument { XmlResolver = null };
		using var reader = XmlReader.Create(filename, settings);
		document.Load(reader);
		return document;
	}

	private static string SerializeXml(XmlDocument document)
	{
		var builder = new StringBuilder();
		using var writer = new UTF8StringWriter(builder);
		document.Save(writer);
		return builder.ToString();
	}

	private static bool IsLoaderTarget(XmlElement element)
	{
		return element.GetAttribute("name") == "EFTTarget"
			&& element.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance") == "EFTTarget";
	}

	private static bool CreateOutline(FileTransaction transaction, Installation installation, ZipArchive archive)
	{
		var outlinePath = Path.Combine(installation.Data, "outline");
		var backupPath = outlinePath + ".ravenx.backup";
		var markerPath = outlinePath + ".ravenx.sha256";
		try
		{
			var entries = archive.Entries
				.Where(entry => entry.Name.Equals(Path.GetFileName(outlinePath), StringComparison.OrdinalIgnoreCase))
				.Take(2)
				.ToArray();
			if (entries.Length == 0)
			{
				AnsiConsole.MarkupLine("[red]Unable to find outline in the zip archive.[/]");
				return false;
			}
			if (entries.Length > 1)
			{
				AnsiConsole.MarkupLine("[red]Multiple outline files were found in the zip archive.[/]");
				return false;
			}

			var entry = entries[0];
			using var hashInput = entry.Open();
			var installedHash = AtomicFile.Hash(hashInput);
			var currentUsable = IsUsableFile(outlinePath);
			var markerOwnsCurrent = currentUsable
				&& AtomicFile.TryReadHash(markerPath, out var previousHash)
				&& string.Equals(previousHash, AtomicFile.Hash(outlinePath), StringComparison.OrdinalIgnoreCase);
			var legacyOwnsCurrent = currentUsable
				&& !File.Exists(markerPath)
				&& IsUsableFile(backupPath)
				&& EntryMatchesFile(entry, outlinePath);

			transaction.Track(outlinePath);
			transaction.Track(backupPath);
			transaction.Track(markerPath);

			if (currentUsable && !markerOwnsCurrent && !legacyOwnsCurrent)
				AtomicFile.Copy(outlinePath, backupPath);

			AtomicFile.Write(outlinePath, temporary =>
			{
				using var input = entry.Open();
				using var output = File.Create(temporary);
				input.CopyTo(output);
			});
			AtomicFile.WriteText(markerPath, installedHash);

			AnsiConsole.MarkupLine($"Created [green]{Path.GetFileName(outlinePath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(outlinePath).EscapeMarkup()}[/].");
			return true;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Unable to create {outlinePath.EscapeMarkup()}: {ex.Message.EscapeMarkup()}.[/]");
			return false;
		}
	}

	private static bool EntryMatchesFile(ZipArchiveEntry entry, string filename)
	{
		var file = new FileInfo(filename);
		if (!file.Exists || file.Length != entry.Length)
			return false;

		using var left = entry.Open();
		using var right = File.OpenRead(filename);
		var leftBuffer = new byte[81920];
		var rightBuffer = new byte[81920];

		while (true)
		{
			var leftRead = left.Read(leftBuffer, 0, leftBuffer.Length);
			var rightRead = right.Read(rightBuffer, 0, rightBuffer.Length);
			if (leftRead != rightRead)
				return false;

			if (leftRead == 0)
				return true;

			for (var i = 0; i < leftRead; i++)
			{
				if (leftBuffer[i] != rightBuffer[i])
					return false;
			}
		}
	}

	private static bool CreateDll(FileTransaction transaction, Installation installation, string filename, Action<string> creator, bool overwrite = true)
	{
		return CreateDll(transaction, installation, filename, s =>
		{
			creator(s);
			return null;
		}, overwrite);
	}

	private static bool CreateDll(FileTransaction transaction, Installation installation, string filename, Func<string, EmitResult?> creator, bool overwrite = true)
	{
		var dllPath = Path.IsPathRooted(filename) ? filename : Path.Combine(installation.Managed, filename);
		var dllPathBepInExCore = Path.IsPathRooted(filename) ? null : Path.Combine(installation.BepInExCore, filename);

		try
		{
			if (!overwrite && dllPathBepInExCore != null && IsUsableFile(dllPathBepInExCore))
			{
				AnsiConsole.MarkupLine($"Using existing [green]{Path.GetFileName(dllPathBepInExCore).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(dllPathBepInExCore).EscapeMarkup()}[/].");
				return true;
			}

			if (!overwrite && IsUsableFile(dllPath))
			{
				AnsiConsole.MarkupLine($"Using existing [green]{Path.GetFileName(dllPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(dllPath).EscapeMarkup()}[/].");
				return true;
			}

			transaction.Track(dllPath);
			AtomicFile.Write(dllPath, temporary =>
			{
				var result = creator(temporary);
				if (result == null)
					return;

				var errors = result.Diagnostics
					.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
					.ToArray();

#if DEBUG
				foreach (var error in errors)
					AnsiConsole.MarkupLine($"[grey]>> {error.Id} [[{error.Location.SourceTree?.FilePath.EscapeMarkup()}]]: {error.GetMessage().EscapeMarkup()}.[/]");
#endif

				if (!result.Success)
					throw new Exception(errors.FirstOrDefault()?.GetMessage() ?? "Unknown error while emitting assembly");
			});

			AnsiConsole.MarkupLine($"Created [green]{Path.GetFileName(dllPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(dllPath).EscapeMarkup()}[/].");
			return true;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Unable to create {dllPath.EscapeMarkup()}: {ex.Message.EscapeMarkup()} [/]");
			return false;
		}
	}

	private static bool IsUsableFile(string filename)
	{
		return File.Exists(filename) && new FileInfo(filename).Length > 0;
	}
}
