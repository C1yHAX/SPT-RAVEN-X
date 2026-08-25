using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Xml;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Installer;

internal sealed class UninstallCommand : Command<UninstallCommand.Settings>
{
	internal class Settings : CommandSettings
	{
		[Description("Path to EFT.")]
		[CommandArgument(0, "[path]")]
		public string? Path { get; set; }
	}

	[SupportedOSPlatform("windows")]
	public override int Execute(CommandContext context, Settings settings, CancellationToken ct)
	{
		try
		{
			ct.ThrowIfCancellationRequested();
			AnsiConsole.MarkupLine("-=[[ [cyan]SPT RAVEN-X Universal Installer[/] - [blue]https://github.com/C1yHAX/SPT-RAVEN-X [/]]]=-");
			AnsiConsole.WriteLine();

			var installation = Installation.GetTargetInstallation(settings.Path, "Please select from where to uninstall RavenX");
			if (installation == null)
				return (int)ExitCode.NoInstallationFound;

			AnsiConsole.MarkupLine($"Target [green]EscapeFromTarkov ({installation.Version})[/] in [blue]{installation.Location.EscapeMarkup()}[/].");

			ct.ThrowIfCancellationRequested();
			if (!RemoveFile(Path.Combine(installation.Managed, "RavenX.dll")))
				return (int)ExitCode.RemoveDllFailed;

			ct.ThrowIfCancellationRequested();
			if (!RestoreOrRemoveOutline(installation))
				return (int)ExitCode.RemoveOutlineFailed;

			ct.ThrowIfCancellationRequested();
			if (!RemoveFile(Path.Combine(installation.BepInExPlugins, "RavenX.Plugin.dll")))
				return (int)ExitCode.RemovePluginDllFailed;

			ct.ThrowIfCancellationRequested();
			if (!RemoveOrPatchConfiguration(installation))
				return (int)ExitCode.RemoveConfigurationFailed;
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

	private static bool RestoreOrRemoveOutline(Installation installation)
	{
		var outline = Path.Combine(installation.Data, "outline");
		var backup = outline + ".ravenx.backup";
		var marker = outline + ".ravenx.sha256";

		try
		{
			if (!File.Exists(marker))
			{
				if (!File.Exists(backup))
					return RemoveFile(outline);

				AtomicFile.Copy(backup, outline);
				File.Delete(backup);
				AnsiConsole.MarkupLine($"Restored [green]{Path.GetFileName(outline).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(outline).EscapeMarkup()}[/].");
				return true;
			}

			if (!AtomicFile.TryReadHash(marker, out var installedHash))
			{
				AnsiConsole.MarkupLine($"[yellow]Unable to verify ownership of {outline.EscapeMarkup()}. The file was left unchanged.[/]");
				return true;
			}

			if (File.Exists(outline) && !string.Equals(AtomicFile.Hash(outline), installedHash, StringComparison.OrdinalIgnoreCase))
			{
				AnsiConsole.MarkupLine($"[yellow]{outline.EscapeMarkup()} was changed after RavenX installed it. The file and its backup were left unchanged.[/]");
				return true;
			}

			if (!File.Exists(backup))
			{
				if (!RemoveFile(outline))
					return false;

				File.Delete(marker);
				return true;
			}

			AtomicFile.Copy(backup, outline);
			File.Delete(backup);
			File.Delete(marker);
			AnsiConsole.MarkupLine($"Restored [green]{Path.GetFileName(outline).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(outline).EscapeMarkup()}[/].");
			return true;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Unable to restore or remove {outline.EscapeMarkup()}: {ex.Message.EscapeMarkup()} [/]");
			return false;
		}
	}

	private static bool RemoveFile(string filename)
	{
		try
		{
			if (!File.Exists(filename))
			{
				AnsiConsole.MarkupLine($"No [green]{Path.GetFileName(filename).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(filename).EscapeMarkup()}[/].");
			}
			else
			{
				File.Delete(filename);
				AnsiConsole.MarkupLine($"Removed [green]{Path.GetFileName(filename).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(filename).EscapeMarkup()}[/].");
			}

			return true;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Unable to remove {filename.EscapeMarkup()}: {ex.Message.EscapeMarkup()} [/]");
			return false;
		}
	}

	private static bool RemoveOrPatchConfiguration(Installation installation)
	{
		const string targetName = "EFTTarget";
		var configPath = Path.Combine(installation.Managed, "NLog.dll.nlog");
		var markerPath = configPath + ".ravenx-created";
		try
		{
			if (!File.Exists(configPath))
			{
				if (File.Exists(markerPath))
					File.Delete(markerPath);

				AnsiConsole.MarkupLine($"No [green]{Path.GetFileName(configPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(configPath).EscapeMarkup()}[/].");
				return true;
			}

			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Prohibit,
				XmlResolver = null
			};
			var doc = new XmlDocument { XmlResolver = null };
			using (var reader = XmlReader.Create(configPath, settings))
				doc.Load(reader);

			var nlogNode = doc.DocumentElement;
			var targetsNode = nlogNode?.ChildNodes.Cast<XmlNode>().OfType<XmlElement>().FirstOrDefault(node => node.LocalName == "targets");

			if (nlogNode?.LocalName != "nlog" || targetsNode == null)
			{
				AnsiConsole.MarkupLine($"[red]Unable to unpatch {configPath.EscapeMarkup()}, unexpected xml structure.[/]");
				return false;
			}

			var removeNodes = targetsNode
				.ChildNodes
				.Cast<XmlNode>()
				.OfType<XmlElement>()
				.Where(element => element.GetAttribute("name") == targetName && element.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance") == targetName)
				.ToList();

			if (removeNodes.Count == 0)
			{
				if (File.Exists(markerPath))
					File.Delete(markerPath);

				AnsiConsole.MarkupLine($"Not patched [green]{Path.GetFileName(configPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(configPath).EscapeMarkup()}[/].");
				return true;
			}

			var removeConfiguration = File.Exists(markerPath) && IsInstallerOwnedConfiguration(doc, nlogNode, targetsNode, removeNodes);

			foreach (var target in removeNodes)
				targetsNode.RemoveChild(target);

			if (!removeConfiguration)
			{
				var builder = new StringBuilder();
				using var writer = new UTF8StringWriter(builder);
				doc.Save(writer);
				AtomicFile.WriteText(configPath, builder.ToString());

				AnsiConsole.MarkupLine($"Unpatched [green]{Path.GetFileName(configPath).EscapeMarkup()}[/] in [blue]{Path.GetDirectoryName(configPath).EscapeMarkup()}[/].");
			}
			else
			{
				if (!RemoveFile(configPath))
					return false;
			}

			if (File.Exists(markerPath))
				File.Delete(markerPath);

			return true;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Unable to unpatch or remove {configPath.EscapeMarkup()}: {ex.Message.EscapeMarkup()}.[/]");
			return false;
		}
	}

	private static bool IsInstallerOwnedConfiguration(XmlDocument document, XmlElement root, XmlElement targets, System.Collections.Generic.IReadOnlyCollection<XmlElement> loaderTargets)
	{
		if (loaderTargets.Count != 1 || HasUnexpectedAttributes(root, true) || HasUnexpectedAttributes(targets, false))
			return false;

		var loader = loaderTargets.First();
		if (loader.Attributes.Count != 2 || loader.GetAttribute("name") != "EFTTarget" || loader.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance") != "EFTTarget")
			return false;

		return HasOnlyExpectedDocumentContent(document, root)
			&& HasOnlyExpectedContent(root, targets)
			&& HasOnlyExpectedContent(targets, loader)
			&& HasOnlyWhitespace(loader);
	}

	private static bool HasOnlyExpectedDocumentContent(XmlDocument document, XmlNode expected)
	{
		return document.ChildNodes.Cast<XmlNode>().All(node => ReferenceEquals(node, expected) || node.NodeType == XmlNodeType.XmlDeclaration || IsWhitespace(node));
	}

	private static bool HasUnexpectedAttributes(XmlElement element, bool allowNamespaces)
	{
		return element.Attributes.Cast<XmlAttribute>().Any(attribute => !allowNamespaces || attribute.Name != "xmlns" && attribute.Prefix != "xmlns");
	}

	private static bool HasOnlyExpectedContent(XmlNode parent, XmlNode expected)
	{
		return parent.ChildNodes.Cast<XmlNode>().All(node => ReferenceEquals(node, expected) || IsWhitespace(node));
	}

	private static bool HasOnlyWhitespace(XmlNode node)
	{
		return node.ChildNodes.Cast<XmlNode>().All(IsWhitespace);
	}

	private static bool IsWhitespace(XmlNode node)
	{
		return node.NodeType is XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace
			|| node.NodeType == XmlNodeType.Text && string.IsNullOrWhiteSpace(node.Value);
	}
}
