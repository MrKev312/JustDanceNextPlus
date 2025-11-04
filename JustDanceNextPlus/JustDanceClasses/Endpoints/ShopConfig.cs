using JustDanceNextPlus.Utilities;

using System.Collections.Immutable;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record ShopConfig
{
	public FirstPartyProductDb FirstPartyProductDb { get; init; } = new();
}

public record FirstPartyProductDb
{
	public ImmutableDictionary<Guid, DlcProduct> DlcProducts { get; init; } = ImmutableDictionary<Guid, DlcProduct>.Empty;
    public ImmutableDictionary<Guid, object> SubscriptionProducts { get; init; } = ImmutableDictionary<Guid, object>.Empty;
    public ImmutableDictionary<Guid, ProductGroup> ProductGroups { get; init; } = ImmutableDictionary<Guid, ProductGroup>.Empty;
}

public record DlcProduct
{
	public ImmutableArray<string> ClaimIds { get; init; } = [];
	public string FirstPartyId { get; init; } = "";
	public string Name { get; init; } = "";
	public required OasisTag ProductLocId { get; init; }
	public string Type { get; init; } = "";
	public string DlcType { get; init; } = "";
	public required OasisTag ProductDescriptionId { get; init; }
}

public record ProductGroup
{
	public string Type { get; init; } = "";
	public int DisplayPriority { get; init; }
	public required OasisTag GroupLocId { get; init; }
	public string Name { get; init; } = "";
	public ImmutableArray<Guid> ProductIds { get; init; } = [];
	public required OasisTag SongsCountLocId { get; init; }
	public required OasisTag GroupDescriptionLocId { get; init; }
	public ImmutableArray<MapTag> TracklistExtended { get; init; } = [];
	public ImmutableArray<MapTag> TracklistLimited { get; init; } = [];
	public required OasisTag TracklistExtendedLocId { get; init; }
	public required OasisTag TracklistLimitedLocId { get; init; }
	public Assets Assets { get; init; } = new();
}

public record Assets
{
	public string ProductGroupBundle { get; init; } = "";
}
