using System;
using RavenX.Features;
using RavenX.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EFT;

#nullable enable

namespace RavenX.Configuration;

public class TrackedItemConverter : JsonConverter
{
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is not TrackedItem item)
			return;

		if (item.Color.HasValue | item.Rarity.HasValue)
			serializer.Serialize(writer, JObject.FromObject(item));
		else
			serializer.Serialize(writer, item.Name);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var value = serializer.Deserialize(reader);
		return value switch
		{
			string name => new TrackedItem(name),
			JObject jobject => jobject.ToObject<TrackedItem>()!,

			// Returning a placeholder entry here used to be harmless because the list was
			// only ever an addition. It now decides whether the overlay is restricted, so
			// one unreadable entry would silently hide every item instead of being read
			// as the empty list it effectively is.
			_ => throw new JsonSerializationException(string.Format(Strings.ErrorCorruptedFileFormat, "ravenx.ini"))
		};
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(TrackedItem);
	}
}
