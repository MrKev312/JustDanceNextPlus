using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class LockerItem
{
	public Guid ItemId { get; set; }
	public Guid SpaceId { get; set; } = Guid.Parse("1da01a17-3bc7-4b5d-aedd-70a0915089b0");
	public required string NameId { get; set; }
	public required string Type { get; set; } // Enum?
	// Todo: Parse this nicely
	public JsonObject? Obj { get; set; }
	public int Revision { get; set; }
	public object? Duration { get; set; }
	public string[] Tags { get; set; } = [];
	public DateTime LastModified { get; set; }
	public int MaximumQuantity { get; set; } = 1;
	public Localizations Localizations { get; set; } = new();
	public LockerAssets Assets { get; set; } = new();
	public string BusinessCategory { get; set; } = "";
	public DateTime? VisibleStartDate { get; set; }
	public DateTime? VisibleEndDate { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Rarity
{
	Common,
	Rare,
	Legendary
}

public class Localizations
{
	public object? NameStringId { get; set; }
	public object? ExcerptStringId { get; set; }
	public object? DescriptionStringId { get; set; }
}

public class LockerAssets
{
	public object? VisualAssetId { get; set; }
	public string VisualAssetUrl { get; set; } = "";
}
