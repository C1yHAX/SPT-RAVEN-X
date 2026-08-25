using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;

namespace Installer;

internal sealed class FileTransaction : IDisposable
{
	private sealed record Snapshot(string Target, string? Backup);

	private readonly string _backupRoot = Path.Combine(Path.GetTempPath(), $"RavenX-Installer-{Guid.NewGuid():N}");
	private readonly HashSet<string> _tracked = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<Snapshot> _snapshots = [];
	private bool _committed;

	public void Track(string filename)
	{
		if (_committed)
			throw new InvalidOperationException("Transaction is already complete");

		var target = Path.GetFullPath(filename);
		if (!_tracked.Add(target))
			return;

		string? backup = null;
		if (File.Exists(target))
		{
			Directory.CreateDirectory(_backupRoot);
			backup = Path.Combine(_backupRoot, $"{_snapshots.Count:D4}.bak");
			File.Copy(target, backup);
		}

		_snapshots.Add(new Snapshot(target, backup));
	}

	public void Commit()
	{
		if (_committed)
			return;

		_committed = true;
		try
		{
			CleanUp();
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[yellow]Unable to remove temporary installation backups: {ex.Message.EscapeMarkup()}[/]");
		}
	}

	public void Dispose()
	{
		if (_committed)
			return;

		Exception? failure = null;
		foreach (var snapshot in _snapshots.AsEnumerable().Reverse())
		{
			try
			{
				if (snapshot.Backup == null)
				{
					if (File.Exists(snapshot.Target))
						File.Delete(snapshot.Target);
				}
				else
				{
					Restore(snapshot.Backup, snapshot.Target);
				}
			}
			catch (Exception ex)
			{
				failure ??= ex;
			}
		}

		if (failure != null)
		{
			_committed = true;
			throw new IOException($"Unable to restore the previous installation state. Backups remain in {_backupRoot}", failure);
		}

		_committed = true;
		try
		{
			CleanUp();
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[yellow]Previous files were restored, but temporary backups could not be removed: {ex.Message.EscapeMarkup()}[/]");
		}

		if (_snapshots.Count > 0)
			AnsiConsole.MarkupLine("[yellow]Installation did not complete. Previous files were restored.[/]");
	}

	private static void Restore(string source, string destination)
	{
		var directory = Path.GetDirectoryName(destination);
		if (string.IsNullOrEmpty(directory))
			throw new IOException("Invalid restore path");

		Directory.CreateDirectory(directory);
		var temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.restore");
		try
		{
			File.Copy(source, temporary);
			if (File.Exists(destination))
				File.Replace(temporary, destination, null);
			else
				File.Move(temporary, destination);
		}
		finally
		{
			if (File.Exists(temporary))
				File.Delete(temporary);
		}
	}

	private void CleanUp()
	{
		foreach (var snapshot in _snapshots)
		{
			if (snapshot.Backup != null && File.Exists(snapshot.Backup))
				File.Delete(snapshot.Backup);
		}

		if (Directory.Exists(_backupRoot))
			Directory.Delete(_backupRoot);
	}
}
