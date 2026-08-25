using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Configuration;

public class ColorConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is not Color color)
		{
			writer.WriteNull();
			return;
		}

		serializer.Serialize(writer, new[] { color.r, color.g, color.b, color.a });
	}

	public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var array = serializer.Deserialize<float[]>(reader);
		var nullable = Nullable.GetUnderlyingType(objectType) == typeof(Color);

		if (array == null)
			return nullable ? null! : throw new JsonSerializationException();

		if (array.Length is not 3 and not 4 || array.Any(float.IsNaN) || array.Any(float.IsInfinity))
			throw new JsonSerializationException();

		return new Color(array[0], array[1], array[2], array.Length == 4 ? array[3] : 1f);
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(Color) || Nullable.GetUnderlyingType(objectType) == typeof(Color);
	}

	public static Color? Parse(string value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		value = value
			.Trim()
			.ToLower();

		var colorType = typeof(Color);
		var field = colorType.GetProperty(value, BindingFlags.Static | BindingFlags.Public);
		if (field != null)
			return (Color)field.GetValue(null);

		try
		{
			return JsonConvert.DeserializeObject<Color>(value, new ColorConverter());
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static string[] ColorNames()
	{
		var colorType = typeof(Color);
		return [.. colorType
			.GetProperties(BindingFlags.Static | BindingFlags.Public)
			.Where(p => p.PropertyType == colorType)
			.Select(p => p.Name)];
	}
}
