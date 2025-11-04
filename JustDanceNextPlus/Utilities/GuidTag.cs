using JustDanceNextPlus.Services;

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Utilities;

[JsonConverter(typeof(GuidTagConverter))]
public record GuidTag(Tag Tag)
{
	public Guid TagGuid => Tag.TagGuid;
}

public record Tag
{
	[JsonIgnore]
	public Guid TagGuid { get; init; } = Guid.Empty;
	public string TagName { get; init; } = "";
	public required OasisTag LocId { get; init; }
	public string Category { get; init; } = "";
	public ImmutableArray<string> Synonyms { get; init; } = [];

	[JsonIgnore]
	public string? Name => LocId.Name;

	// Allow implicit conversion from GuidTag to Guid
	public static implicit operator Guid(Tag tag) => tag.TagGuid;
	public static implicit operator GuidTag(Tag tag) => new(tag);
}

public class GuidTagConverter(ITagService tagService,
    ILocalizedStringService localizedStringService) : JsonConverter<GuidTag>
{
	public override GuidTag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		string tagString = reader.GetString() ?? throw new JsonException();

		Tag tag;
		if (Guid.TryParse(tagString, out Guid tagGuid))
		{
			// Exception for PadamALT's second to last tag, which somehow doesn't exist.
			tag = tagGuid == Guid.Parse("68cb4e62-5040-46ff-bdd2-1bf6c3c8d357")
				? new Tag { TagGuid = tagGuid, LocId = localizedStringService.GetLocalizedTag(0)!, }
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
