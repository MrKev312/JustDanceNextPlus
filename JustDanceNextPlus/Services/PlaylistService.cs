using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public interface IPlaylistService : ILoadService
{
	PlaylistDB PlaylistDB { get; set; }
	PlaylistDB GetPlaylistDB(string platform);
}

public class PlaylistService(IMapService mapService,
	JsonSettingsService jsonSettingsService,
	IOptions<PathSettings> pathSettings,
	IOptions<UrlSettings> urlSettings,
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

			string playlistName = string.IsNullOrWhiteSpace(playlist.PlaylistName)
				? Path.GetFileNameWithoutExtension(file)
				: playlist.PlaylistName;

			JustDancePlaylist playlistFinal = ((JustDancePlaylist)playlist) with
			{
				PlaylistName = playlistName,
				Assets = BuildPlaylistAssets(ClientPlatform.DefaultPlatform, playlistName)
			};

            List<Itemlist> itemList = [.. playlistFinal.ItemList];

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

                itemList.AddRange(maps);

            }

			// If the playlist has an order by, sort the maps
			if (playlist.OrderBy != null)
			{
                itemList = [.. itemList.Select(x => mapService.Songs[x.Id])
					.AsQueryable()
					.OrderBy(playlist.OrderBy)
					.Select(x => new Itemlist
					{
						Id = mapService.MapToGuid[x.MapName],
                        Type = "map"
                    })];
            }

            playlistFinal = playlistFinal with
            {
                ItemList = [.. itemList.DistinctBy(x => x.Id)]
            };

            if (playlistFinal.ItemList == null || playlistFinal.ItemList.Length <= 1)
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

	public PlaylistDB GetPlaylistDB(string platform)
	{
		return new PlaylistDB
		{
			Playlists = new SortedDictionary<Guid, JustDancePlaylist>(
				PlaylistDB.Playlists.ToDictionary(
					kvp => kvp.Key,
					kvp => kvp.Value with
					{
						Assets = BuildPlaylistAssets(platform, kvp.Value.PlaylistName)
					})),
			ShowcasePlaylists = [.. PlaylistDB.ShowcasePlaylists],
			DynamicPlaylists = [.. PlaylistDB.DynamicPlaylists],
			PlaylistsOffers = new PlaylistsOffers
			{
				FreePlaylists = [.. PlaylistDB.PlaylistsOffers.FreePlaylists],
				AvailablePlaylists = [.. PlaylistDB.PlaylistsOffers.AvailablePlaylists],
				VisiblePlaylists = [.. PlaylistDB.PlaylistsOffers.VisiblePlaylists],
				HiddenPlaylists = [.. PlaylistDB.PlaylistsOffers.HiddenPlaylists],
				Subscribed = [.. PlaylistDB.PlaylistsOffers.Subscribed]
			}
		};
	}

	private PlaylistAssets BuildPlaylistAssets(string platform, string playlistName)
	{
		return new PlaylistAssets
		{
			En = new LocalizedAssets
			{
				Cover = BuildBundleUrl(platform, playlistName),
				CoverDetails = BuildBundleUrl(platform, $"{playlistName}_details")
			}
		};
	}

	private string BuildBundleUrl(string platform, string bundleName)
		=> $"{CdnBaseUrl}/bundles/{ClientPlatform.Normalize(platform)}/{NormalizeBundleName(bundleName)}.bundle";

	private string CdnBaseUrl
	{
		get
		{
			string trimmedUrl = urlSettings.Value.CDNUrl.Trim().TrimEnd('/');
			return trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				|| trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
				? trimmedUrl
				: $"https://{trimmedUrl}";
		}
	}

	private static string NormalizeBundleName(string bundleName)
	{
		string normalizedName = bundleName.Trim().Replace('\\', '/');
		if (Uri.TryCreate(normalizedName, UriKind.Absolute, out Uri? uri))
			normalizedName = uri.LocalPath;

		return Path.GetFileNameWithoutExtension(normalizedName);
	}
}

public class PlaylistDB
{
	public SortedDictionary<Guid, JustDancePlaylist> Playlists { get; set; } = [];
	public List<Guid> ShowcasePlaylists { get; set; } = [];
	public List<Guid> DynamicPlaylists { get; set; } = [];
	public PlaylistsOffers PlaylistsOffers { get; set; } = new();
}

public record JustDancePlaylist
{
	[JsonIgnore]
	public Guid Guid { get; init; } = Guid.Empty;
	public string PlaylistName { get; init; } = "Placeholder";
	public ImmutableArray<Itemlist> ItemList { get; init; } = [];
	public string ListSource { get; init; } = "editorial";
	public int LocalizedTitle { get; init; }
	public int LocalizedDescription { get; init; }
	public string DefaultLanguage { get; init; } = "en";
	public PlaylistAssets Assets { get; init; } = new();
	public ImmutableArray<GuidTag> Tags { get; init; } = [];
	public ImmutableArray<object> OffersTags { get; init; } = [];
	public bool Hidden { get; init; }
}

public record PlaylistAssets
{
	public LocalizedAssets En { get; init; } = new();
}

public record LocalizedAssets
{
	public string Cover { get; init; } = "Placeholder";
	public string CoverDetails { get; init; } = "Placeholder";
}

public record Itemlist
{
	public Guid Id { get; init; }
	public string Type { get; init; } = "Placeholder";
}

public class PlaylistsOffers
{
	public List<Guid> FreePlaylists { get; set; } = [];
	public List<Guid> AvailablePlaylists { get; set; } = [];
	public List<Guid> VisiblePlaylists { get; set; } = [];
	public List<Guid> HiddenPlaylists { get; set; } = [];
	public List<Guid> Subscribed { get; set; } = [];
}
