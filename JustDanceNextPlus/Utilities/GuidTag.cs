using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class GuidTag
{
	public Guid Guid { get; set; }

	// Allow implicit conversion from GuidTag to Guid
	public static implicit operator Guid(GuidTag tag) => tag.Guid;
	public static implicit operator GuidTag(Guid tag) => new() { Guid = tag };
}

public class GuidTagConverter(TagService tagService) : JsonConverter<GuidTag>
{
	public override GuidTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// If it's not a string, throw an exception
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException();

		// Convert the JSON string to a Guid
		string tagString = reader.GetString() ?? throw new JsonException();
		if (Guid.TryParse(tagString, out Guid tagGuid))
			return new GuidTag { Guid = tagGuid };

		// If it's not a Guid, try to get the tag from the tag service
		Guid tag = tagService.GetAddTag(tagString, "tag");

		return new GuidTag { Guid = tag };
	}
	public override void Write(Utf8JsonWriter writer, GuidTag value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.Guid.ToString());
	}
}
