using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

// Class as stored in the database
public class JsonPlaylist
{
	public Guid Guid { get; set; } = Guid.Empty;
	public string PlaylistName { get; set; } = "";
	public string ListSource { get; set; } = "editorial";
	public OasisTag LocalizedTitle { get; set; } = new();
	public OasisTag LocalizedDescription { get; set; } = new();
	public string DefaultLanguage { get; set; } = "en";
	public string CoverUrl { get; set; } = "";
	public string CoverDetailsUrl { get; set; } = "";
	public List<GuidTag> Tags { get; set; } = [];
	public bool Hidden { get; set; } = false;

	// The maps
	public List<MapTag> ItemList { get; set; } = [];

	// Implicit conversion to Playlist
	public static implicit operator JustDancePlaylist(JsonPlaylist playlist)
	{
		JustDancePlaylist newPlaylist = new()
		{
			Guid = playlist.Guid,
			PlaylistName = playlist.PlaylistName,
			ListSource = playlist.ListSource,
			LocalizedTitle = playlist.LocalizedTitle,
			LocalizedDescription = playlist.LocalizedDescription,
			DefaultLanguage = playlist.DefaultLanguage,
			Assets = new PlaylistAssets
			{
				En = new LocalizedAssets
				{
					Cover = playlist.CoverUrl,
					CoverDetails = playlist.CoverDetailsUrl
				}
			},
			Tags = playlist.Tags,
			Hidden = playlist.Hidden
		};

		foreach (MapTag map in playlist.ItemList)
			newPlaylist.ItemList.Add(new Itemlist { Id = map.Guid, Type = "map" });

		return newPlaylist;
	}
}
