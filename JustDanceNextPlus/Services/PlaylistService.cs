using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Services;

public class PlaylistService(
	IServiceProvider serviceProvider,
	ILogger<PlaylistService> logger)
{
    public PlaylistDB PlaylistDB { get; set; } = new();

	public void LoadData()
    {
		IOptions<PathSettings> pathSettings = serviceProvider.GetRequiredService<IOptions<PathSettings>>();
		JsonSettingsService jsonSettingsService = serviceProvider.GetRequiredService<JsonSettingsService>();

		logger.LogInformation("Loading playlists");

        string path = pathSettings.Value.PlaylistPath;

        if (!Directory.Exists(path))
        {
            logger.LogWarning("Playlist path does not exist, will not load playlists");
            return;
        }

        string[] files = Directory.GetFiles(path, "*.json");

        foreach (string file in files)
        {
            logger.LogInformation($"Loading playlist {file}");

            string json = File.ReadAllText(file);
			JsonPlaylist? playlist = JsonSerializer.Deserialize<JsonPlaylist>(json, jsonSettingsService.PrettyPascalFormat);

            if (playlist == null)
            {
                logger.LogWarning($"Failed to load playlist {file}");
                continue;
            }

            if (playlist.ItemList == null || playlist.ItemList.Count <= 1)
            {
                logger.LogWarning($"Playlist {file} has no songs, skipping");
                continue;
            }

			PlaylistDB.Playlists[playlist.Guid] = playlist;
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
