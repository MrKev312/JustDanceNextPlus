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
	public int ProductLocId { get; set; }
	public string Type { get; set; } = "";
	public string DlcType { get; set; } = "";
	public int ProductDescriptionId { get; set; }
}

public class ProductGroup
{
	public string Type { get; set; } = "";
	public int DisplayPriority { get; set; }
	public int GroupLocId { get; set; }
	public string Name { get; set; } = "";
	public List<Guid> ProductIds { get; set; } = [];
	public int SongsCountLocId { get; set; }
	public int GroupDescriptionLocId { get; set; }
	public List<Guid> TracklistExtended { get; set; } = [];
	public List<Guid> TracklistLimited { get; set; } = [];
	public int TracklistExtendedLocId { get; set; }
	public int TracklistLimitedLocId { get; set; }
	public Assets Assets { get; set; } = new();
}

public class Assets
{
	public string ProductGroupBundle { get; set; } = "";
}
