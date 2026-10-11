using JustDanceServer.JustDanceClasses.Database;

using System.Text.Json;

namespace JustDanceServer.Services;

public interface IServerMapLoader
{
    ValueTask<ServerMap> LoadMapAsync(string mapFolder);
}

public sealed class ServerMapLoader(
    JsonSettingsService jsonSettingsService,
    ILogger<ServerMapLoader> logger,
    IFileSystem fileSystem,
    IMapMediaLoader mediaLoader,
    IMapPackageReader mapPackageReader) : IServerMapLoader
{
    private static readonly string[] VideoCodecs = ["vp8", "vp9"];

    public async ValueTask<ServerMap> LoadMapAsync(string mapFolder)
    {
        try
        {
            LocalJustDanceSongDBEntry song = await LoadSongAsync(mapFolder);
            IReadOnlyDictionary<string, MapMediaAssets> media = MapMediaLayouts.All.ToDictionary(
                layout => layout.FolderName,
                layout => mediaLoader.LoadMedia(mapFolder, layout),
                StringComparer.OrdinalIgnoreCase);
            IReadOnlyDictionary<string, MapVideoSet> fullVideos = VideoCodecs.ToDictionary(
                codec => codec,
                codec => mediaLoader.LoadVideoSet(mapFolder, $"Video/{codec}", codec),
                StringComparer.OrdinalIgnoreCase);

            return new ServerMap(song.SongID, mapFolder, song, media, fullVideos);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load map: ({MapFolder})", mapFolder);
            throw new InvalidOperationException($"Failed to load map: ({mapFolder})", exception);
        }
    }

    private async ValueTask<LocalJustDanceSongDBEntry> LoadSongAsync(string mapFolder)
    {
        string songInfoPath = Path.Combine(mapFolder, "SongInfo.json");
        if (!fileSystem.FileExists(songInfoPath))
            throw new FileNotFoundException($"Missing SongInfo.json in {mapFolder}");

        using Stream stream = fileSystem.OpenRead(songInfoPath);
        LocalJustDanceSongDBEntry song =
            (await JsonSerializer.DeserializeAsync<LocalJustDanceSongDBEntry>(stream, jsonSettingsService.PrettyPascalFormat))
            ?? throw new InvalidOperationException($"Failed to deserialize SongInfo.json in {mapFolder}");
        song = NormalizeSongInfoForBoot(song, mapFolder, logger);

        if (string.IsNullOrEmpty(song.MapName))
            throw new InvalidOperationException($"MapName is null or empty in {songInfoPath}");

        string mapPackagePath = FindSharedMetadataMapPackage(mapFolder);
        MapPackageMetadata packageMetadata = mapPackageReader.ReadMetadata(mapPackagePath);
        song = song with
        {
            HasCameraScoring = packageMetadata.HasCameraScoring,
            AssetsMetadata = new AssetsMetadata { AudioPreviewTrk = packageMetadata.AudioPreviewTrack }
        };

        if (!song.Tags.Any(tag => tag.Equals("jdplus", StringComparison.OrdinalIgnoreCase)
            || tag.StartsWith("songpack_", StringComparison.OrdinalIgnoreCase)
            || tag.StartsWith("Music_Pack_", StringComparison.OrdinalIgnoreCase)))
        {
            song = song with { Tags = song.Tags.Add("jdplus") };
        }

        return song;
    }

    private string FindSharedMetadataMapPackage(string mapFolder)
    {
        string assetsFolder = Path.Combine(mapFolder, "JDPlus", "Assets");
        foreach (string platformFolder in fileSystem.GetDirectories(assetsFolder)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            string mapPackageFolder = Path.Combine(platformFolder, "MapPackage");
            if (!fileSystem.DirectoryExists(mapPackageFolder))
                continue;

            string? mapPackage = fileSystem.GetFiles(mapPackageFolder)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (mapPackage is not null)
                return mapPackage;
        }

        throw new FileNotFoundException($"Missing JDPlus map package under {assetsFolder}.");
    }

    internal static LocalJustDanceSongDBEntry NormalizeSongInfoForBoot(
        LocalJustDanceSongDBEntry song,
        string mapFolder,
        ILogger logger)
    {
        if (song.SweatDifficulty != 0)
            return song;

        logger.LogWarning(
            "Map '{MapName}' in '{MapFolder}' has sweatDifficulty 0; defaulting to 1.",
            string.IsNullOrWhiteSpace(song.MapName) ? Path.GetFileName(mapFolder) : song.MapName,
            mapFolder);
        return song with { SweatDifficulty = 1 };
    }
}
