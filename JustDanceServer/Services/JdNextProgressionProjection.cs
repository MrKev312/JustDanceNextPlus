using JustDanceServer.JustDanceClasses.Database.Profile;

namespace JustDanceServer.Services;

// Shared persistence uses map names; JDNext's REST contracts still use song GUIDs.
public static class JdNextProgressionProjection
{
    public static object? ToProfile(Profile? profile, IMapService maps)
        => profile is null ? null : new
        {
            profile.Id,
            profile.CurrentXP,
            profile.CurrentLevel,
            profile.PrestigeGrade,
            profile.MapStat,
            profile.Ownership,
            MapStats = ToSongIds(profile.MapStats, maps),
            PlaylistStats = profile.PlaylistStats.ToDictionary(
                pair => pair.Key,
                pair => ToPlaylistStats(pair.Value, maps)),
            profile.MapHistory,
            profile.PlaylistHistory,
            profile.RunningTasks,
            profile.CompletedTasks,
            profile.ObjectiveCompletionData,
            profile.BossModeStats
        };

    public static JdNextPlaylistStats? ToPlaylistStats(PlaylistStats? stats, IMapService maps)
        => stats is null ? null : new(
            stats.HighScore,
            stats.PlayCount,
            stats.Platform,
            stats.UpdatedAt,
            ToSongIds(stats.HighScorePerMap, maps));

    private static Dictionary<Guid, T> ToSongIds<T>(IReadOnlyDictionary<string, T> scores, IMapService maps)
    {
        Dictionary<Guid, T> result = [];
        foreach ((string mapName, T score) in scores)
        {
            // A shared/JDU score may belong to a map absent from the JDNext catalog.
            if (maps.GetSongId(mapName) is Guid songId)
                result[songId] = score;
        }
        return result;
    }
}

public sealed record JdNextPlaylistStats(
    int HighScore,
    int PlayCount,
    string Platform,
    DateTime UpdatedAt,
    Dictionary<Guid, int> HighScorePerMap);
