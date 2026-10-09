using JustDanceServer.Services;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

public sealed class Template
{
	[JsonPropertyName("template")]
	public List<TemplateItem> TemplateItems { get; set; } =
	[
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("song", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("song", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("song", "carousel")
	];
}

public sealed class TemplateItem
{
	public TemplateItem(string categoryType)
	{
		Categorytype = categoryType;
	}

	public TemplateItem(string itemType, string categoryType)
	{
		Itemtype = itemType;
		Categorytype = categoryType;
	}

	public string Itemtype { get; set; } = "";
	public string Bannertype { get; set; } = "";
	public string Categorytype { get; set; } = "";
}

public sealed class PublicKeyPinning
{
	public List<PinningKey> PinningKeys { get; set; } = [];
}

public sealed class PinningKey
{
	public string Endpoint { get; set; } = "";
	public string Description { get; set; } = "";
	public List<string> PublicKeyDigests { get; set; } = [];
}

public sealed class Filters(ITagService tagService)
{
	[JsonPropertyName("filters")]
	public List<Filter> FilterList { get; set; } = tagService.GetFilters();
}
