using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public interface IPlaylistService : ILoadService
{
	PlaylistDB PlaylistDB { get; set; }
}

public class PlaylistService(IMapService mapService,
	JsonSettingsService jsonSettingsService,
	IOptions<PathSettings> pathSettings,
	ILogger<PlaylistService> logger,
    IFileSystem fileSystem) : IPlaylistService, ILoadService
{
	public PlaylistDB PlaylistDB { get; set; } = new();

	public async Task LoadData()
	{
		string path = pathSettings.Value.PlaylistPath;

		if (!fileSystem.DirectoryExists(path))
		{
			logger.LogWarning("Playlist path does not exist, will not load playlists");
			return;
		}

		string[] files = fileSystem.GetFiles(path, "*.json");

		foreach (string file in files)
		{
			using Stream fileStream = fileSystem.OpenRead(file);
			JsonPlaylist? playlist = await JsonSerializer.DeserializeAsync<JsonPlaylist>(fileStream, jsonSettingsService.PrettyPascalFormat);

			if (playlist == null)
			{
				logger.LogWarning("Failed to load playlist {File}", file);
				continue;
			}

			JustDancePlaylist playlistFinal = playlist;

			// If the playlist has a query, run the query on the map database and get the maps
			if (playlist.Query != null)
			{
				List<Itemlist> maps = [.. mapService.Songs.Select(x => x.Value).AsQueryable()
					.Where(playlist.Query)
					.Select(x => x.MapName)
					.Select(x=> new Itemlist
					{
						Id = mapService.MapToGuid[x],
						Type = "map"
					})];

				playlistFinal.ItemList.AddRange(maps);
			}

			// If the playlist has an order by, sort the maps
			if (playlist.OrderBy != null)
			{
				playlistFinal.ItemList = [.. playlistFinal.ItemList.Select(x => mapService.Songs[x.Id])
					.AsQueryable()
					.OrderBy(playlist.OrderBy)
					.Select(x => new Itemlist
					{
						Id = mapService.MapToGuid[x.MapName],
						Type = "map"
					})];
			}

			if (playlistFinal.ItemList == null || playlistFinal.ItemList.Count <= 1)
			{
				logger.LogWarning("Playlist {File} has not enough maps, skipping", file);
				continue;
			}

			PlaylistDB.Playlists[playlist.Guid] = playlistFinal;
			PlaylistDB.PlaylistsOffers.AvailablePlaylists.Add(playlist.Guid);
			PlaylistDB.PlaylistsOffers.VisiblePlaylists.Add(playlist.Guid);
		}

		logger.LogInformation("Finished loading playlists");
	}
}

public class PlaylistDB
{
	public SortedDictionary<Guid, JustDancePlaylist> Playlists { get; set; } = [];
	public List<Guid> ShowcasePlaylists { get; set; } = [];
	public List<Guid> DynamicPlaylists { get; set; } = [];
	public PlaylistsOffers PlaylistsOffers { get; set; } = new();
}

public class JustDancePlaylist
{
	[JsonIgnore]
	public Guid Guid { get; set; } = Guid.Empty;
	public string PlaylistName { get; set; } = "Placeholder";
	public List<Itemlist> ItemList { get; set; } = [];
	public string ListSource { get; set; } = "editorial";
	public int LocalizedTitle { get; set; }
	public int LocalizedDescription { get; set; }
	public string DefaultLanguage { get; set; } = "en";
	public PlaylistAssets Assets { get; set; } = new();
	public List<GuidTag> Tags { get; set; } = [];
	public List<object> OffersTags { get; set; } = [];
	public bool Hidden { get; set; }
}

public class PlaylistAssets
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

public class PlaylistsOffers
{
	public List<Guid> FreePlaylists { get; set; } = [];
	public List<Guid> AvailablePlaylists { get; set; } = [];
	public List<Guid> VisiblePlaylists { get; set; } = [];
	public List<Guid> HiddenPlaylists { get; set; } = [];
	public List<Guid> Subscribed { get; set; } = [];
}
