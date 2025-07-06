using JustDanceNextPlus.Utilities;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class ShopConfig
{
	public FirstPartyProductDb FirstPartyProductDb { get; set; } = new();
}

public class FirstPartyProductDb
{
	public Dictionary<Guid, DlcProduct> DlcProducts { get; set; } = [];
	public Dictionary<Guid, object> SubscriptionProducts { get; set; } = [];
	public Dictionary<Guid, ProductGroup> ProductGroups { get; set; } = [];
}

public class DlcProduct
{
	public List<string> ClaimIds { get; set; } = [];
	public string FirstPartyId { get; set; } = "";
	public string Name { get; set; } = "";
	public required OasisTag ProductLocId { get; set; }
	public string Type { get; set; } = "";
	public string DlcType { get; set; } = "";
	public required OasisTag ProductDescriptionId { get; set; }
}

public class ProductGroup
{
	public string Type { get; set; } = "";
	public int DisplayPriority { get; set; }
	public required OasisTag GroupLocId { get; set; }
	public string Name { get; set; } = "";
	public List<Guid> ProductIds { get; set; } = [];
	public required OasisTag SongsCountLocId { get; set; }
	public required OasisTag GroupDescriptionLocId { get; set; }
	public List<MapTag> TracklistExtended { get; set; } = [];
	public List<MapTag> TracklistLimited { get; set; } = [];
	public required OasisTag TracklistExtendedLocId { get; set; }
	public required OasisTag TracklistLimitedLocId { get; set; }
	public Assets Assets { get; set; } = new();
}

public class Assets
{
	public string ProductGroupBundle { get; set; } = "";
}
