using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class OasisTag(LocalizedString localizedString)
{
	public LocalizedString LocalizedString { get; set; } = localizedString;

	public int ID => LocalizedString.OasisIdInt;
	public string? Name => LocalizedString.DisplayString;

	// Allow implicit conversion from OasisTag to int
	public static implicit operator int(OasisTag tag) => tag.ID;
	public static implicit operator OasisTag(LocalizedString localizedString) => new(localizedString);
}

public class LocalizedString
{
	[JsonIgnore]
	public int OasisIdInt { get; set; }
	public string OasisId { get => OasisIdInt.ToString(); set => OasisIdInt = int.Parse(value); }
	public string LocaleCode { get; set; } = "";
	public string DisplayString { get; set; } = "";
	public Guid LocalizedStringId { get; set; }
	public object? Obj { get; set; }
	public int SpaceRevision { get; set; } = 121;
}

public class TagIdConverter(LocalizedStringService localizedStringService) : JsonConverter<OasisTag>
{
	public override OasisTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		LocalizedString? localizedString;

		// If we're reading an integer, just return it as an OasisTag
		switch (reader.TokenType)
		{
			case JsonTokenType.Number:
				int tagId = reader.GetInt32();
				localizedString = localizedStringService.GetLocalizedTag(tagId)
					?? throw new JsonException($"Tag ID {tagId} not found in localized strings database.");

				break;
			case JsonTokenType.String:
				// If we're reading a string, we need to convert it to an int using the localized string service
				string read = reader.GetString() ?? throw new JsonException("Tag string is null");
				localizedString = localizedStringService.GetAddLocalizedTag(read);
				break;
			default:
				throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		return new OasisTag(localizedString);
	}

	public override void Write(Utf8JsonWriter writer, OasisTag value, JsonSerializerOptions options)
	{
		writer.WriteNumberValue(value.ID);
	}
}
