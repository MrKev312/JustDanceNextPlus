using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public record MapTag
{
	public Guid Guid { get; init; }

	// Allow implicit conversion from GuidTag to Guid
	public static implicit operator Guid(MapTag tag) => tag.Guid;
	public static implicit operator MapTag(Guid tag) => new() { Guid = tag };
}

public class MapTagConverter(IMapService mapService, ILogger<MapTagConverter> logger) : JsonConverter<MapTag>
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
		Guid? tag = mapService.GetSongId(tagString);

		if (tag == null)
		{
			// If the tag service doesn't know the tag, log a warning and return null
			logger.LogWarning("Unknown tag: {Tag}", tagString);
			return new MapTag { Guid = Guid.Empty };
		}

		return new MapTag { Guid = tag.Value };
	}

	public override void Write(Utf8JsonWriter writer, MapTag value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.Guid.ToString());
	}
}

// Parse any IEnumerable of MapTags
public class MapTagListConverter(IMapService mapService, ILogger<MapTagListConverter> logger) : JsonConverter<List<MapTag>>
{
	public override List<MapTag>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// If it's not an array, throw an exception
		if (reader.TokenType != JsonTokenType.StartArray)
			throw new JsonException();

		List<MapTag> tags = [];
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray)
				return tags;

			// If it's not a string, throw an exception
			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException();

			// Get the string
			string tagString = reader.GetString() ?? throw new JsonException();

			// Is it a Guid?
			if (Guid.TryParse(tagString, out Guid tagGuid))
				tags.Add(new MapTag { Guid = tagGuid });
			else
			{
				// Does the map service know this tag?
				Guid? tag = mapService.GetSongId(tagString);

				// No? Then skip it
				if (tag == null)
				{
					logger.LogWarning("Unknown tag: {Tag}", tagString);
					continue;
				}

				// Add the tag
				tags.Add(new MapTag { Guid = tag.Value });
			}
		}

		return tags;
	}
	public override void Write(Utf8JsonWriter writer, List<MapTag> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (MapTag tag in value)
			writer.WriteStringValue(tag.Guid.ToString());
		writer.WriteEndArray();
	}
}