using System.Text.Json.Serialization;
using System.Text.Json;
using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.Utilities;

public class MapTag
{
	public Guid Guid { get; set; }

	// Allow implicit conversion from GuidTag to Guid
	public static implicit operator Guid(MapTag tag) => tag.Guid;
	public static implicit operator MapTag(Guid tag) => new() { Guid = tag };
}

public class MapTagConverter(MapService mapService) : JsonConverter<MapTag>
{
	public override MapTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// If it's not a string, throw an exception
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException();

		// Convert the JSON string to a Guid
		string tagString = reader.GetString() ?? throw new JsonException();
		if (Guid.TryParse(tagString, out Guid tagGuid))
			return new MapTag { Guid = tagGuid };

		// If it's not a Guid, try to get the tag from the tag service
		Guid tag = mapService.MapToGuid[tagString];

		return new MapTag { Guid = tag };
	}

	public override void Write(Utf8JsonWriter writer, MapTag value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.Guid.ToString());
	}
}
