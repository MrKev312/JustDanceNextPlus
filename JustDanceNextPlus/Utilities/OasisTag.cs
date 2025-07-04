using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class OasisTag
{
	public int ID { get; set; } = 0;
	public string? Name { get; set; }

	// Allow implicit conversion from OasisTag to int
	public static implicit operator int(OasisTag tag) => tag.ID;
	public static implicit operator OasisTag(int tag) => new() { ID = tag };
}

public class TagIdConverter(LocalizedStringService localizedStringService) : JsonConverter<OasisTag>
{
	public override OasisTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		int tagId;
		string? tagString;

		// If we're reading an integer, just return it as an OasisTag
		switch (reader.TokenType)
		{
			case JsonTokenType.Number:
				tagId = reader.GetInt32();
				if (tagId == 0)
					tagString = null; // Handle the case where tagId is 0
				else
				{
					tagString = localizedStringService.GetLocalizedTag(tagId)?.DisplayString;
					if (tagString == null)
					{
						throw new JsonException($"Tag ID {tagId} not found in localized strings database.");
					}
				}

				break;
			case JsonTokenType.String:
				// If we're reading a string, we need to convert it to an int using the localized string service
				tagString = reader.GetString() ?? throw new JsonException("Tag string is null");
				tagId = localizedStringService.GetAddLocalizedTag(tagString).OasisIdInt;
				break;
			default:
				throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		return new OasisTag { ID = tagId, Name = tagString };
	}

	public override void Write(Utf8JsonWriter writer, OasisTag value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value.ID);
	}
}
