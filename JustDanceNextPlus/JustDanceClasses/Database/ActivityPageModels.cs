using JustDanceNextPlus.Utilities;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class ActivityPageResponse
{
	public Dictionary<Guid, ICategory> Categories { get; set; } = [];

	public List<CategoryModifier> CategoryModifiers { get; set; } = [];
}

public interface ICategory
{
	string CategoryName { get; set; }

	string Type { get; }

	OasisTag TitleId { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	bool? DoNotFilterOwnership { get; set; }
}

public class BannerCategory : ICategory
{
	public string CategoryName { get; set; } = string.Empty;
	public string Type => "banner";
	public required OasisTag TitleId { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? DoNotFilterOwnership { get; set; }

	[JsonPropertyName("bannerType")]
	public string BannerType { get; set; } = string.Empty;

	[JsonPropertyName("descriptionId")]
	public required OasisTag DescriptionId { get; set; }

	[JsonPropertyName("assets")]
	public required CategoryAssets Assets { get; set; }

	[JsonPropertyName("item")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Item? Item { get; set; }
}

public class CarouselCategory : ICategory
{
	public string CategoryName { get; set; } = string.Empty;
	public string Type => "carousel";
	public required OasisTag TitleId { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? DoNotFilterOwnership { get; set; }

	public string ListSource { get; set; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<Filter>? Filters { get; set; }

	public string RecommendedItemType { get; set; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<MapTag>? ItemList { get; set; }
}

public class CategoryAssets
{
	public string BackgroundImage { get; set; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ForegroundImage { get; set; }
}

public class Item
{
	public MapTag Id { get; set; } = new();

	public string Type { get; set; } = string.Empty;
}

public class Filter
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<GuidTag>? IncludedTags { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<GuidTag>? ExcludedTags { get; set; }

	public string ItemTypeToFilter { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? AlreadyPlayedMap { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ComparedValue? ComparedValue { get; set; }
}

public class ComparedValue
{
	public string Name { get; set; } = string.Empty;

	public int Value { get; set; }
}

public class CategoryModifier
{
	public string ItemTypeToModify { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public PositionInPage Position { get; set; } = new();
}

public class PositionInPage
{
	public Guid Id { get; set; }

	public int Position { get; set; }
}