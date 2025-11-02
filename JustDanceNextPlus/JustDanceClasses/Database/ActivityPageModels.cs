using JustDanceNextPlus.Utilities;

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public record ActivityPageResponse
{
    public ImmutableDictionary<Guid, ICategory> Categories { get; init; } = ImmutableDictionary<Guid, ICategory>.Empty;

    public ImmutableList<IModifier> CategoryModifiers { get; init; } = [];
}

[JsonConverter(typeof(ICategoryConverter))]
public interface ICategory
{
    string CategoryName { get; }

    string Type { get; }

    OasisTag TitleId { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? DoNotFilterOwnership { get; }
}

public record BannerCategory : ICategory
{
    public string CategoryName { get; init; } = string.Empty;
    public string Type => "banner";
    public required OasisTag TitleId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DoNotFilterOwnership { get; init; }

    [JsonPropertyName("bannerType")]
    public string BannerType { get; init; } = string.Empty;

    [JsonPropertyName("descriptionId")]
    public required OasisTag DescriptionId { get; init; }

    [JsonPropertyName("assets")]
    public required CategoryAssets Assets { get; init; }

    [JsonPropertyName("item")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Item? Item { get; init; }
}

public record CarouselCategory : ICategory
{
    public string CategoryName { get; init; } = string.Empty;
    public string Type => "carousel";
    public required OasisTag TitleId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DoNotFilterOwnership { get; init; }

    public string ListSource { get; init; } = "editorial";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableList<Filter>? Filters { get; init; }

    public string RecommendedItemType { get; init; } = "map";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableList<MapTag>? ItemList { get; init; }
}

public record CategoryAssets
{
    public string BackgroundImage { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ForegroundImage { get; init; }
}

public record Item
{
    public MapTag Id { get; init; } = new();

    public string Type { get; init; } = string.Empty;
}

public record Filter
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableList<GuidTag>? IncludedTags { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableList<GuidTag>? ExcludedTags { get; init; }

    public string ItemTypeToFilter { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AlreadyPlayedMap { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ComparedValue? ComparedValue { get; init; }
}

public record ComparedValue
{
    public string Name { get; init; } = string.Empty;

    public int Value { get; init; }
}

[JsonConverter(typeof(IModifierConverter))]
public interface IModifier
{
    public string Name { get; }
}

public class IModifierConverter : JsonConverter<IModifier>
{
    public override IModifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token.");
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        string? type = root.GetProperty("name").GetString();
        if (type == null)
            throw new JsonException("Modifier type is missing.");
        return type switch
        {
            "positionModifier" => JsonSerializer.Deserialize<PositionModifier>(root.GetRawText(), options)!,
            "maxAmountElement" => JsonSerializer.Deserialize<MaxAmountElements>(root.GetRawText(), options)!,
            "moreThanXElement" => JsonSerializer.Deserialize<MoreThanXElements>(root.GetRawText(), options)!,
            _ => throw new JsonException($"Unknown modifier type: {type}"),
        };
    }
    public override void Write(Utf8JsonWriter writer, IModifier value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

public record PositionModifier : IModifier
{
    public PositionModifier() { }
    public PositionModifier(string type, Guid guid, int position)
    {
        ItemTypeToModify = type;
        Position = new PositionInPage { Id = guid, Position = position };
    }

    public string ItemTypeToModify { get; init; } = string.Empty;

    public string Name { get; init; } = "position";

    public PositionInPage Position { get; init; } = new();

    public record PositionInPage
    {
        public Guid Id { get; init; }

        public int Position { get; init; }
    }
}

public record MaxAmountElements : IModifier
{
    public string ItemTypeToFilter { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MaxAmountElementValue MaxAmountElement { get; init; } = new();

    public record MaxAmountElementValue
    {
        public string Type { get; init; } = string.Empty;
        public int Value { get; init; } = 0;
    }
}

public record MoreThanXElements : IModifier
{
    public string ItemTypeToModify { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Value { get; init; } = 0;
}
