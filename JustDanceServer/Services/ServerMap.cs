using JustDanceServer.JustDanceClasses.Database;
namespace JustDanceServer.Services;

public sealed record ServerMap(
    Guid SongId,
    string FolderPath,
    JustDanceSongDBEntry Song,
    IReadOnlyDictionary<string, MapMediaAssets> Media,
    IReadOnlyDictionary<string, MapVideoSet> FullVideos)
{
    public MapMediaAssets GetMedia(MapMediaLayout layout)
        => Media.TryGetValue(layout.FolderName, out MapMediaAssets? media)
            ? media
            : throw new InvalidDataException($"Map '{Song.MapName}' is missing {layout.FolderName} media.");

    public MapVideoSet GetFullVideos(string codec)
        => FullVideos.TryGetValue(codec, out MapVideoSet? videos)
            ? videos
            : throw new InvalidDataException($"Map '{Song.MapName}' is missing its required {codec} full videos.");
}
