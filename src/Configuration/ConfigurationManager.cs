using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RavenX.Features;
using RavenX.Properties;
using EFT.UI;
using JsonType;
using Newtonsoft.Json;
using EFT;

#nullable enable

namespace RavenX.Configuration;

internal static class ConfigurationManager
{
	private static readonly JsonConverter[] _converters = [new TrackedItemConverter(), new ColorConverter(), new KeyCodeConverter(), new EnumConverter<ELootRarity>(), new EnumConverter<BotDifficulty>()];

	public static JsonConverter[] Converters => _converters;

	private static void AddConsoleLog(string log)
	{
		if (PreloaderUI.Instantiated)
			ConsoleScreen.Log(log);
	}

	public static bool Load(string filename, Feature[] features, bool warnIfNotExists = true)
	{
		try
		{
			if (!File.Exists(filename))
			{
				if (warnIfNotExists)
					AddConsoleLog(string.Format(Strings.ErrorFileNotFoundFormat, filename).Red());

				return false;
			}

			var values = ReadValues(filename);
			var loaded = true;
			var pending = new List<(Feature Feature, PropertyInfo Property, object Value)>();

			foreach (var feature in features)
			{
				var featureType = feature.GetType();
				var properties = GetOrderedProperties(featureType);

				foreach (var op in properties)
				{
					var key = $"{featureType.FullName}.{op.Property.Name}";
					try
					{
						if (!values.TryGetValue(key, out var serialized))
							continue;

						var value = Deserialize(serialized, op.Property.PropertyType);
						pending.Add((feature, op.Property, value));
					}
					catch (Exception)
					{
						loaded = false;
						AddConsoleLog(string.Format(Strings.ErrorCorruptedPropertyFormat, key, filename).Red());
					}
				}
			}

			if (!loaded)
				return false;

			foreach (var entry in pending)
				entry.Property.SetValue(entry.Feature, entry.Value);

			AddConsoleLog(string.Format(Strings.CommandLoadSuccessFormat, filename));
			return true;
		}
		catch (Exception exception)
		{
			AddConsoleLog(string.Format(Strings.ErrorCannotLoadFormat, filename, exception.Message).Red());
			return false;
		}
	}

	public static bool LoadPropertyValue(string filename, Feature feature, string propertyName)
	{
		try
		{
			if (!File.Exists(filename))
			{
				AddConsoleLog(string.Format(Strings.ErrorFileNotFoundFormat, filename).Red());
				return false;
			}

			var text = File.ReadAllText(filename);
			var property = GetOrderedProperties(feature.GetType())
				.First(p => p.Property.Name == propertyName)
				.Property;

			try
			{
				var value = Deserialize(text, property.PropertyType);
				property.SetValue(feature, value);
			}
			catch (Exception)
			{
				AddConsoleLog(string.Format(Strings.ErrorCorruptedFileFormat, filename).Red());
				return false;
			}

			AddConsoleLog(string.Format(Strings.CommandLoadSuccessFormat, filename));
			return true;
		}
		catch (Exception exception)
		{
			AddConsoleLog(string.Format(Strings.ErrorCannotLoadFormat, filename, exception.Message).Red());
			return false;
		}
	}

	public static bool Save(string filename, Feature[] features)
	{
		try
		{
			var content = new StringBuilder();
			content.AppendLine(Comment(Strings.CommandSaveHeader));
			content.AppendLine();

			foreach (var feature in features.OrderBy(f => f.GetType().FullName))
			{
				var featureType = feature.GetType();
				var properties = GetOrderedProperties(featureType);

				foreach (var op in properties)
				{
					var key = $"{featureType.FullName}.{op.Property.Name}";
					var value = JsonConvert.SerializeObject(op.Property.GetValue(feature), Formatting.None, Converters);

					var resourceId = op.Attribute.CommentResourceId;
					if (!string.IsNullOrEmpty(resourceId))
						content.AppendLine(Comment(Strings.ResourceManager.GetString(resourceId)));

					content.AppendLine($"{key}={value}");
				}

				if (properties.Length > 0)
					content.AppendLine();
			}

			WriteAtomic(filename, content.ToString());
			AddConsoleLog(string.Format(Strings.CommandSaveSuccessFormat, filename));
			return true;
		}
		catch (Exception exception)
		{
			AddConsoleLog(string.Format(Strings.ErrorCannotSaveFormat, filename, exception.Message).Red());
			return false;
		}
	}

	private static string Comment(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		const string commentToken = "; ";
		const string resxNewLine = "\n";
		return commentToken + value!.Replace(resxNewLine, resxNewLine + commentToken);
	}

	public static bool SavePropertyValue(string filename, Feature feature, string propertyName)
	{
		try
		{
			var property = GetOrderedProperties(feature.GetType())
				.First(p => p.Property.Name == propertyName)
				.Property;

			var content = JsonConvert.SerializeObject(property.GetValue(feature), Formatting.Indented, Converters);
			WriteAtomic(filename, content);

			AddConsoleLog(string.Format(Strings.CommandSaveSuccessFormat, filename));
			return true;
		}
		catch (Exception exception)
		{
			AddConsoleLog(string.Format(Strings.ErrorCannotSaveFormat, filename, exception.Message).Red());
			return false;
		}
	}

	private static object Deserialize(string value, Type propertyType)
	{
		var result = JsonConvert.DeserializeObject(value, propertyType, Converters);
		if (result == null)
			throw new JsonSerializationException();

		return result;
	}

	private static Dictionary<string, string> ReadValues(string filename)
	{
		var values = new Dictionary<string, string>(StringComparer.Ordinal);

		foreach (var line in File.ReadAllLines(filename))
		{
			var separator = line.IndexOf('=');
			if (separator <= 0)
				continue;

			var key = line.Substring(0, separator).Trim();
			if (key.Length == 0 || key[0] == ';')
				continue;

			values[key] = line.Substring(separator + 1);
		}

		return values;
	}

	internal static void WriteAtomic(string filename, string content)
	{
		var target = Path.GetFullPath(filename);
		var directory = Path.GetDirectoryName(target);
		if (string.IsNullOrEmpty(directory))
			throw new IOException();

		Directory.CreateDirectory(directory);
		var temporary = Path.Combine(directory, $".{Path.GetFileName(target)}.{Guid.NewGuid():N}.tmp");

		try
		{
			File.WriteAllText(temporary, content, new UTF8Encoding(false));

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

	public static bool IsSkippedProperty(Feature feature, string name)
	{
		return IsSkippedProperty(feature.GetType(), name);
	}

	public static bool IsSkippedProperty(Type featureType, string name)
	{
		var property = featureType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		if (property == null)
			return false;

		var attribute = property.GetCustomAttribute<ConfigurationPropertyAttribute>(true);
		return attribute is { Skip: true };
	}

	public static OrderedProperty[] GetOrderedProperties(Type featureType)
	{
		var properties = featureType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

		return
		[
			.. properties
			.Select(p => new { property = p, attribute = p.GetCustomAttribute<ConfigurationPropertyAttribute>(true) })
			.Where(p => p.attribute is { Skip: false } && p.property.CanRead && p.property.CanWrite)
			.Select(op => new OrderedProperty(op.attribute!, op.property))
			.OrderBy(op => op.Attribute.Order)
			.ThenBy(op => op.Property.Name)
		];
	}
}
