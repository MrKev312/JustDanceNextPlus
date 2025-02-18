using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class PlaylistService(
	IServiceProvider serviceProvider,
	ILogger<PlaylistService> logger)
{
    public List<JustDancePlaylist> Playlists { get; set; } = [];

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
            JustDancePlaylist? playlist = JsonSerializer.Deserialize<JustDancePlaylist>(json, jsonSettingsService.PrettyPascalFormat);

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

            Playlists.Add(playlist);
        }
    }
}
