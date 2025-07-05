using JustDanceNextPlus.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

public class Tag
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
	public static implicit operator Guid(Tag tag) => tag.TagGuid;
	public static implicit operator GuidTag(Tag tag) => new(tag);
}

[JsonConverter(typeof(GuidTagConverter))]
public class GuidTag(Tag tag)
{
	public Tag Tag { get; set; } = tag;
	public Guid TagGuid => Tag.TagGuid;
}

public class GuidTagConverter(TagService tagService) : JsonConverter<GuidTag>
{
	public override GuidTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string tagString = reader.GetString() ?? throw new JsonException();

		Tag tag;
		if (Guid.TryParse(tagString, out Guid tagGuid))
		{
			// Exception for PadamALT's second to last tag, which somehow doesn't exist.
			tag = tagGuid == Guid.Parse("68cb4e62-5040-46ff-bdd2-1bf6c3c8d357")
				? new Tag { TagGuid = tagGuid }
				: tagService.GetTag(tagGuid)
					?? throw new JsonException($"Tag with GUID {tagGuid} not found in the database.");
		}
		else
		{
			tag = tagService.GetAddTag(tagString, "tag");
		}

		return new GuidTag(tag);
	}

	public override void Write(Utf8JsonWriter writer, GuidTag value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.TagGuid.ToString());
	}
}
