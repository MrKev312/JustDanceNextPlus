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
	public OasisTag ProductLocId { get; set; } = 0;
	public string Type { get; set; } = "";
	public string DlcType { get; set; } = "";
	public OasisTag ProductDescriptionId { get; set; } = 0;
}

public class ProductGroup
{
	public string Type { get; set; } = "";
	public int DisplayPriority { get; set; }
	public OasisTag GroupLocId { get; set; } = 0;
	public string Name { get; set; } = "";
	public List<Guid> ProductIds { get; set; } = [];
	public OasisTag SongsCountLocId { get; set; } = 0;
	public OasisTag GroupDescriptionLocId { get; set; } = 0;
	public List<MapTag> TracklistExtended { get; set; } = [];
	public List<MapTag> TracklistLimited { get; set; } = [];
	public OasisTag TracklistExtendedLocId { get; set; } = 0;
	public OasisTag TracklistLimitedLocId { get; set; } = 0;
	public Assets Assets { get; set; } = new();
}

public class Assets
{
	public string ProductGroupBundle { get; set; } = "";
}
