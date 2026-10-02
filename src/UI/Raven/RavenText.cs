using System.Collections.Generic;
using RavenX.Properties;
using EFT;

#nullable enable

namespace RavenX.UI.Raven;

internal static class RavenText
{
	private const string Prefix = "Menu_";

	private static readonly Dictionary<string, string> _cache = [];

	public static string L(string english)
	{
		if (english.Length == 0)
			return english;

		if (_cache.TryGetValue(english, out var cached))
			return cached;

		var translated = english;

		try
		{
			var value = Strings.ResourceManager.GetString(Prefix + Key(english), Strings.Culture);
			if (!string.IsNullOrEmpty(value))
				translated = value!;
		}
		catch
		{
		}

		_cache[english] = translated;
		return translated;
	}

	public static void Clear()
	{
		_cache.Clear();
	}

	internal static string Key(string english)
	{
		var builder = new System.Text.StringBuilder(english.Length + 9);
		var upper = true;

		foreach (var character in english)
		{
			if (char.IsLetterOrDigit(character))
			{
				if (builder.Length < 40)
					builder.Append(upper ? char.ToUpperInvariant(character) : character);

				upper = false;
				continue;
			}

			upper = true;
		}

		unchecked
		{
			var hash = 2166136261u;

			foreach (var character in english)
			{
				hash ^= character;
				hash *= 16777619u;
			}

			builder.Append('_').Append(hash.ToString("x8"));
		}

		return builder.ToString();
	}
}
