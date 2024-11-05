using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.playlistdb.v2.playlistdb.formatVersion;

[ApiController]
[Route("playlistdb/v2/playlistdb/formatVersion/v0")]
public class V0(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetPlaylistDb()
	{
		return Ok(System.IO.File.ReadAllText(Path.Combine(pathSettings.Value.JsonsPath, "playlistdb.json")));
	}
}

public class PlaylistDB
{
	public SortedDictionary<Guid, Playlist> Playlists { get; set; } = [];
	public List<Guid> ShowcasePlaylists { get; set; } = [];
	public List<Guid> DynamicPlaylists { get; set; } = [];
	public PlaylistOffers PlaylistOffers { get; set; } = new();
}

public class PlaylistOffers
{
	public List<Guid> FreePlaylists { get; set; } = [];
	public List<Guid> AvailablePlaylists { get; set; } = [];
	public List<Guid> VisiblePlaylists { get; set; } = [];
	public List<Guid> HiddenPlaylists { get; set; } = [];
	public List<Guid> Subscribed { get; set; } = [];
}

public class Playlist
{
	public string PlaylistName { get; set; } = "Placeholder";
	public List<Itemlist> ItemList { get; set; } = [];
	public string ListSource { get; set; } = "editorial";
	public int LocalizedTitle { get; set; }
	public int LocalizedDescription { get; set; }
	public string DefaultLanguage { get; set; } = "en";
	public Assets Assets { get; set; } = new();
	public List<Guid> Tags { get; set; } = [];
	public object[] OffersTags { get; set; } = [];
	public bool Hidden { get; set; }
}

public class Assets
{
	public LocalizedAssets En { get; set; } = new();
}

public class LocalizedAssets
{
	public string Cover { get; set; } = "Placeholder";
	public string CoverDetails { get; set; } = "Placeholder";
}

public class Itemlist
{
	public Guid Id { get; set; }
	public string Type { get; set; } = "Placeholder";
}
