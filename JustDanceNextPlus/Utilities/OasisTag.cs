using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class OasisTag
{
	public int ID { get; set; }

	// Allow implicit conversion from OasisTag to int
	public static implicit operator int(OasisTag tag) => tag.ID;
	public static implicit operator OasisTag(int tag) => new() { ID = tag };
}

public class TagIdConverter(LocalizedStringService localizedStringService) : JsonConverter<OasisTag>
{
	public override OasisTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// If we're reading an integer, just return it as an OasisTag
		if (reader.TokenType == JsonTokenType.Number)
			return new OasisTag { ID = reader.GetInt32() };

		// If it's not a string, throw an exception
		if (reader.TokenType != JsonTokenType.String)
			throw new JsonException();

		// Convert the JSON string to an int using the localized string service
		string tagString = reader.GetString() ?? throw new JsonException();

		// Convert the string to an int
		int tagId = localizedStringService.GetAddLocalizedTag(tagString).OasisIdInt;
		return new OasisTag { ID = tagId };
	}

	public override void Write(Utf8JsonWriter writer, OasisTag value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value.ID);
	}
}
