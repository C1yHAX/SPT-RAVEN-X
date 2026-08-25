using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using Spectre.Console;

namespace Installer;

internal class Installation
{
	public Version Version { get; }
	public bool UsingSpt { get; private set; }
	public bool UsingSptButNeverRun { get; private set; }
	public bool UsingBepInEx { get; private set; }
	public string Location { get; }
	private string? _displayString;
	public string DisplayString => _displayString ??= ComputeDisplayString();

	public string Data => Path.Combine(Location, "EscapeFromTarkov_Data");
	public string Managed => Path.Combine(Data, "Managed");
	public string BepInEx => Path.Combine(Location, "BepInEx");
	public string BepInExCore => Path.Combine(BepInEx, "core");
	public string BepInExPlugins => Path.Combine(BepInEx, "plugins");

	private Installation(string location, Version version)
	{
		if (string.IsNullOrEmpty(location))
			throw new ArgumentException("empty location");

		Location = Path.TrimEndingDirectorySeparator(Path.GetFullPath(location));
		Version = version;
	}

	public override bool Equals(object? obj)
	{
		if (obj is not Installation other)
			return false;

		return string.Equals(other.Location, Location, StringComparison.OrdinalIgnoreCase);
	}

	public override int GetHashCode()
	{
		return StringComparer.OrdinalIgnoreCase.GetHashCode(Location);
	}

	[SupportedOSPlatform("windows")]
	public static Installation? GetTargetInstallation(string? path, string promptTitle)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			Installation? explicitInstallation = null;
			AnsiConsole.Status().Start("Validating [green]Escape From Tarkov[/] installation...", _ => TryDiscoverInstallation(path, out explicitInstallation));

			if (explicitInstallation == null)
			{
				AnsiConsole.MarkupLine($"[yellow]No valid [green]EscapeFromTarkov[/] installation found at [blue]{path.EscapeMarkup()}[/].[/]");
				return null;
			}

			return AnsiConsole.Confirm($"Continue with [green]EscapeFromTarkov ({explicitInstallation.Version})[/] in [blue]{explicitInstallation.Location.EscapeMarkup()}[/] ?")
				? explicitInstallation
				: null;
		}

		var installations = new List<Installation>();

		AnsiConsole
			.Status()
			.Start("Discovering [green]Escape From Tarkov[/] installations...", _ =>
			{
				installations = [.. DiscoverInstallations().Distinct()];
			});

		installations = [.. installations.Distinct().OrderBy(i => i.Location)];

		switch (installations.Count)
		{
			case 0:
				AnsiConsole.MarkupLine("[yellow]No [green]EscapeFromTarkov[/] installation found, please re-run this installer, passing the installation path as argument.[/]");
				return null;
			case 1:
				var first = installations.First();
				return AnsiConsole.Confirm($"Continue with [green]EscapeFromTarkov ({first.Version})[/] in [blue]{first.Location.EscapeMarkup()}[/] ?") ? first : null;
			default:
				var prompt = new SelectionPrompt<Installation> { Title = promptTitle };
				prompt.AddChoices(installations);
				return AnsiConsole.Prompt(prompt);
		}
	}

	[SupportedOSPlatform("windows")]
	private static IEnumerable<Installation> DiscoverInstallations()
	{
		if (TryDiscoverInstallation(Environment.CurrentDirectory, out var installation))
			yield return installation;

		if (TryDiscoverInstallation(Path.GetDirectoryName(AppContext.BaseDirectory), out installation))
			yield return installation;

		if (TryDiscoverInstallation(Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))!, "SPT"), out installation))
			yield return installation;

		foreach (var sptpath in Registry.GetSptInstallationsFromMuiCache())
		{
			if (string.IsNullOrEmpty(sptpath))
				continue;

			if (TryDiscoverInstallation(sptpath, out installation))
				yield return installation;

			if (TryDiscoverInstallation(Path.Combine(sptpath, ".."), out installation))
				yield return installation;
		}

		if (!Registry.TryGetEscapeFromTarkovInstallationPath(out var path))
			yield break;

		if (TryDiscoverInstallation(path, out installation))
			yield return installation;

		string[] subFolders;
		try
		{
			subFolders = [.. Directory.EnumerateDirectories(Path.Combine(path, ".."))];
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
		{
			yield break;
		}

		foreach (var folder in subFolders)
		{
			if (TryDiscoverInstallation(folder, out installation))
				yield return installation;
		}
	}

	private static bool TryDiscoverInstallation(string? path, [NotNullWhen(true)] out Installation? installation)
	{
		installation = null;

		try
		{
			if (string.IsNullOrEmpty(path))
				return false;

			path = Path.GetFullPath(path.Trim('\"'));
			if (File.Exists(path) && Path.GetFileName(path).Equals("EscapeFromTarkov.exe", StringComparison.OrdinalIgnoreCase))
				path = Path.GetDirectoryName(path);

			if (string.IsNullOrEmpty(path))
				return false;

			var exe = Path.Combine(path, "EscapeFromTarkov.exe");
			if (!File.Exists(exe))
				return false;

			var vi = FileVersionInfo.GetVersionInfo(exe);
			if (vi.FileVersion == null)
				return false;

			installation = new Installation(path, new Version(vi.FileVersion));

			if (!Directory.Exists(installation.Managed))
				return false;

			installation.UsingSpt = Directory.Exists(Path.Combine(path, "SPT_Data"))
									|| Directory.Exists(Path.Combine(path, "SPT", "SPT_Data"))
									|| Directory.Exists(Path.Combine(path, "SPT_Runtime", "SPT_Data"));

			var battleye = Path.Combine(path, "BattlEye");
			installation.UsingSptButNeverRun = installation.UsingSpt && Directory.Exists(battleye);

			installation.UsingBepInEx = Directory.Exists(installation.BepInExPlugins);

			return true;
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or FormatException or Win32Exception or System.Security.SecurityException)
		{
			return false;
		}
	}

	private string ComputeDisplayString()
	{
		var sb = new StringBuilder();
		sb.Append($"{Location.EscapeMarkup()} - [[{Version}]] ");
		sb.Append(UsingSpt ? "[b]SPT[/]" : "Vanilla");

		return sb.ToString();
	}

	public override string ToString()
	{
		return DisplayString;
	}
}
