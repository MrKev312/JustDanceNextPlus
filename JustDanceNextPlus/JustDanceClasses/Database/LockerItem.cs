using System.Collections.Immutable;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public record LockerItem
{
	public Guid ItemId { get; init; }
	public Guid SpaceId { get; init; } = Guid.Parse("1da01a17-3bc7-4b5d-aedd-70a0915089b0");
	public required string NameId { get; init; }
	public required string Type { get; init; } // Enum?
	// Todo: Parse this nicely
	public JsonObject? Obj { get; init; }
	public int Revision { get; init; }
	public object? Duration { get; init; }
	public ImmutableArray<string> Tags { get; init; } = [];
	public DateTime LastModified { get; init; }
	public int MaximumQuantity { get; init; } = 1;
	public Localizations Localizations { get; init; } = new();
	public LockerAssets Assets { get; init; } = new();
	public string BusinessCategory { get; init; } = "";
	public DateTime? VisibleStartDate { get; init; }
	public DateTime? VisibleEndDate { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Rarity
{
	Common,
	Rare,
	Legendary
}

public record Localizations
{
	public object? NameStringId { get; init; }
	public object? ExcerptStringId { get; init; }
	public object? DescriptionStringId { get; init; }
}

public record LockerAssets
{
	public object? VisualAssetId { get; init; }
	public string VisualAssetUrl { get; init; } = "";
}
