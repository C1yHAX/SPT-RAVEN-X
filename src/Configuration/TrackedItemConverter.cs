using System;
using RavenX.Features;
using RavenX.Properties;
using JsonType;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using EFT;

#nullable enable

namespace RavenX.Configuration;

public class TrackedItemConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is not TrackedItem item)
		{
			writer.WriteNull();
			return;
		}

		if (!item.Color.HasValue && !item.Rarity.HasValue)
		{
			serializer.Serialize(writer, item.Name);
			return;
		}

		writer.WriteStartObject();
		writer.WritePropertyName(nameof(TrackedItem.Name));
		serializer.Serialize(writer, item.Name);

		if (item.Color.HasValue)
		{
			writer.WritePropertyName(nameof(TrackedItem.Color));
			serializer.Serialize(writer, item.Color.Value);
		}

		if (item.Rarity.HasValue)
		{
			writer.WritePropertyName(nameof(TrackedItem.Rarity));
			serializer.Serialize(writer, item.Rarity.Value);
		}

		writer.WriteEndObject();
	}

	public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var value = JToken.Load(reader);
		if (value.Type == JTokenType.String)
		{
			var simpleName = value.Value<string>();
			if (string.IsNullOrWhiteSpace(simpleName))
				throw new JsonSerializationException(string.Format(Strings.ErrorCorruptedFileFormat, "ravenx.ini"));

			return new TrackedItem(simpleName!);
		}

		if (value is not JObject jobject)
			throw new JsonSerializationException(string.Format(Strings.ErrorCorruptedFileFormat, "ravenx.ini"));

		var name = jobject.TryGetValue(nameof(TrackedItem.Name), StringComparison.OrdinalIgnoreCase, out var nameToken)
			? nameToken.Value<string>()
			: null;
		if (string.IsNullOrWhiteSpace(name))
			throw new JsonSerializationException(string.Format(Strings.ErrorCorruptedFileFormat, "ravenx.ini"));

		Color? color = null;
		if (jobject.TryGetValue(nameof(TrackedItem.Color), StringComparison.OrdinalIgnoreCase, out var colorToken) && colorToken.Type != JTokenType.Null)
			color = colorToken.ToObject<Color>(serializer);

		ELootRarity? rarity = null;
		if (jobject.TryGetValue(nameof(TrackedItem.Rarity), StringComparison.OrdinalIgnoreCase, out var rarityToken) && rarityToken.Type != JTokenType.Null)
			rarity = rarityToken.ToObject<ELootRarity>(serializer);

		return new TrackedItem(name!, color, rarity);
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(TrackedItem);
	}
}
