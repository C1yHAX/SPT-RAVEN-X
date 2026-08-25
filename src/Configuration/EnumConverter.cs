using System;
using Newtonsoft.Json;
using EFT;

#nullable enable

namespace RavenX.Configuration;

public class EnumConverter<T> : JsonConverter where T : struct, Enum
{
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		value ??= default(T)!;
		serializer.Serialize(writer, Enum.GetName(typeof(T), value));
	}

	public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		var value = serializer.Deserialize<string>(reader);
		if (value == null || !Enum.TryParse(value, true, out T result) || !Enum.IsDefined(typeof(T), result))
			throw new JsonSerializationException();

		return result;
	}

	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(T);
	}
}
