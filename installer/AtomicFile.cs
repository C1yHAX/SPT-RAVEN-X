using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Installer;

internal static class AtomicFile
{
	public static void Write(string filename, Action<string> writer)
	{
		var target = Path.GetFullPath(filename);
		var directory = Path.GetDirectoryName(target);
		if (string.IsNullOrEmpty(directory))
			throw new IOException("Invalid output path");

		Directory.CreateDirectory(directory);
		var temporary = Path.Combine(directory, $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.tmp");

		try
		{
			writer(temporary);

			if (!File.Exists(temporary) || new FileInfo(temporary).Length == 0)
				throw new IOException("No output was created");

			if (File.Exists(target))
				File.Replace(temporary, target, null);
			else
				File.Move(temporary, target);
		}
		finally
		{
			if (File.Exists(temporary))
				File.Delete(temporary);
		}
	}

	public static void WriteText(string filename, string content)
	{
		Write(filename, temporary => File.WriteAllText(temporary, content, new UTF8Encoding(false)));
	}

	public static void Copy(string source, string destination)
	{
		Write(destination, temporary => File.Copy(source, temporary));
	}

	public static string Hash(string filename)
	{
		using var stream = File.OpenRead(filename);
		return Hash(stream);
	}

	public static string Hash(Stream stream)
	{
		using var algorithm = SHA256.Create();
		return Convert.ToHexString(algorithm.ComputeHash(stream));
	}

	public static bool TryReadHash(string filename, out string hash)
	{
		hash = string.Empty;
		if (!File.Exists(filename))
			return false;

		var value = File.ReadAllText(filename).Trim();
		if (value.Length != 64 || !value.All(Uri.IsHexDigit))
			return false;

		hash = value.ToUpperInvariant();
		return true;
	}
}
