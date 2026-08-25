using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Installer
{
	[SupportedOSPlatform("windows")]
	internal class Registry
	{
		public static bool TryGetEscapeFromTarkovInstallationPath([NotNullWhen(true)] out string? installationPath)
		{
			installationPath = null;

			try
			{
				using var hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
				using var eft = hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\EscapeFromTarkov", false);

				if (eft == null)
					return false;

				var exe = NormalizeExecutablePath(eft.GetValue("DisplayIcon") as string);
				if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
					return false;

				var path = Path.GetDirectoryName(exe);
				if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
					return false;

				installationPath = path;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static IEnumerable<string?> GetSptInstallationsFromMuiCache()
		{
			try
			{
				using var hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
				using var mui = hive.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache", false);

				if (mui == null)
					return [];

				const string attribute = ".FriendlyAppName";
				string[] candidates = ["SPT.Launcher.exe", "SPT.Server.exe"];

				return mui
					.GetValueNames()
					.Where(v => candidates.Any(c => v.Contains($"{c}{attribute}", StringComparison.OrdinalIgnoreCase)))
					.Select(v => Path.GetDirectoryName(v.Replace(attribute, string.Empty, StringComparison.OrdinalIgnoreCase)))
					.Distinct();
			}
			catch
			{
				return [];
			}
		}

		private static string? NormalizeExecutablePath(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var path = value.Trim();
			if (path[0] == '"')
			{
				var closingQuote = path.IndexOf('"', 1);
				if (closingQuote > 1)
					return path.Substring(1, closingQuote - 1);
			}

			var separator = path.LastIndexOf(',');
			if (separator > 0 && int.TryParse(path.AsSpan(separator + 1), out _))
				path = path.Substring(0, separator);

			return path.Trim().Trim('"');
		}
	}
}
