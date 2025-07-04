using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class GuidTag
{
	[JsonIgnore]
	public Guid TagGuid { get; set; } = Guid.Empty;
	public string TagName { get; set; } = "";
	public OasisTag LocId { get; set; } = new();
	public string Category { get; set; } = "";
	public List<string> Synonyms { get; set; } = [];

	[JsonIgnore]
	public string? Name => LocId.Name;

	// Allow implicit conversion from GuidTag to Guid
	public static implicit operator Guid(GuidTag tag) => tag.TagGuid;

}

public class GuidTagConverter(TagService tagService) : JsonConverter<List<GuidTag>>
{
	public override List<GuidTag> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
			throw new JsonException("Expected start of array.");

		List<GuidTag> tags = [];

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray)
				break;

			if (reader.TokenType != JsonTokenType.String)
				throw new JsonException("Expected string elements in array.");

			string tagString = reader.GetString() ?? throw new JsonException();

			GuidTag tag;
			if (Guid.TryParse(tagString, out Guid tagGuid))
			{
				// Exception for PadamALT's second to last tag, which somehow doesn't exist.
				tag = tagGuid == Guid.Parse("68cb4e62-5040-46ff-bdd2-1bf6c3c8d357")
					? new GuidTag { TagGuid = tagGuid }
					: tagService.GetTag(tagGuid)
						?? throw new JsonException($"Tag with GUID {tagGuid} not found in the database.");
			}
			else
			{
				tag = tagService.GetAddTag(tagString, "tag");
			}

			tags.Add(tag);
		}

		return tags;
	}

	public override void Write(Utf8JsonWriter writer, List<GuidTag> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (GuidTag tag in value)
			writer.WriteStringValue(tag.TagGuid.ToString());

		writer.WriteEndArray();
	}
}
