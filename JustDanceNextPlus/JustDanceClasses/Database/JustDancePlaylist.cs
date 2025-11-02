using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using System.Collections.Immutable;

namespace JustDanceNextPlus.JustDanceClasses.Database;

// Class as stored in the database
public record JsonPlaylist
{
	public Guid Guid { get; init; } = Guid.Empty;
	public string PlaylistName { get; init; } = "";
	public string ListSource { get; init; } = "editorial";
	public required OasisTag LocalizedTitle { get; init; }
	public required OasisTag LocalizedDescription { get; init; }
	public string DefaultLanguage { get; init; } = "en";
	public string CoverUrl { get; init; } = "";
	public string CoverDetailsUrl { get; init; } = "";
	public ImmutableArray<GuidTag> Tags { get; init; } = [];
	public bool Hidden { get; init; } = false;

	// The maps
	public string? Query { get; init; }
	public string? OrderBy { get; init; }
	public ImmutableArray<MapTag> ItemList { get; init; } = [];

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
			Hidden = playlist.Hidden,
			ItemList = [.. playlist.ItemList.Select(map => new Itemlist { Id = map.Guid, Type = "map" })]
		};

		return newPlaylist;
	}
}
