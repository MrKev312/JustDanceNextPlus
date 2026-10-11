using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public readonly record struct ScoreUpdateResult(bool Success, bool IsNewHighScore);

public interface IUserProfileService
{
    ValueTask<bool> AddProfileAsync(Profile profile);
    ValueTask<Guid> GenerateAddProfileAsync(Profile profile);
    ValueTask<Profile?> GetProfileByIdAsync(Guid id);
    ValueTask<IReadOnlyDictionary<Guid, Profile>> GetProfilesByIdsAsync(IReadOnlyCollection<Guid> ids);
    ValueTask<Profile?> GetProfileByTicketAsync(string ticket);
    ValueTask<bool> HasJustDanceProfileAsync(Guid id);
    ValueTask<bool> UpdateProfileAsync(Profile profile);
}

public interface ILeaderboardService
{
    ValueTask<Leaderboard> GetLeaderboardAroundAsync(string mapName, Guid userId, int limit = 3);
    ValueTask<Leaderboard> GetLeaderboardAsync(string mapName, long limit, long offset);
    ValueTask<Leaderboard> GetLeaderboardFromIdsAsync(string mapName, IReadOnlyCollection<Guid> userIds, long limit = 100);
    ValueTask<Leaderboard> GetCountryLeaderboardAsync(string mapName, int countryId, long limit);
    ValueTask<LeaderboardPage> GetDetailedLeaderboardAsync(
        string mapName,
        long limit,
        int? countryId = null,
        IReadOnlyCollection<Guid>? profileIds = null);
    ValueTask<Leaderboard> GetPlaylistLeaderboardAroundAsync(Guid playlistId, Guid userId, int limit = 3);
    ValueTask<Leaderboard> GetPlaylistLeaderboardByPlaylistIdAsync(Guid playlistId, long limit, long offset);
    ValueTask<Leaderboard> GetPlaylistLeaderboardFromIdsAsync(Guid playlistId, List<Guid> userIds);
    ValueTask<Leaderboard> GetRandomOpponentAsync(string mapName, Guid playerId);
}

public interface IScoreService
{
    ValueTask<MapStats?> GetHighScoreByProfileIdAndMapNameAsync(Guid profileId, string mapName);
    ValueTask<List<MapStats>> GetHighScoresByProfileIdAsync(Guid profileId);
    ValueTask<PlaylistStats?> GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(Guid profileId, Guid playlistId);
    ValueTask<List<PlaylistStats>> GetPlaylistHighScoresByProfileIdAsync(Guid profileId);
    ValueTask<ScoreUpdateResult> RecordMapPlayAsync(
        HighscorePerformance highScore,
        int score,
        Guid playerId,
        string mapName,
        string platform,
        bool isCoopHighScore = false);
    ValueTask<ScoreUpdateResult> MergeMapScoreAsync(
        HighscorePerformance highScore,
        int score,
        int playCount,
        Guid playerId,
        string mapName,
        string platform,
        bool isCoopHighScore = false);
    ValueTask<ScoreUpdateResult> UpdatePlaylistHighScoreAsync(
        PlaylistStats playlistStats,
        int score,
        Guid playerId,
        Guid playlistId);
}

public interface IUserDataService : IUserProfileService, ILeaderboardService, IScoreService;

public sealed class UserDataService(
    IUserProfileService profiles,
    ILeaderboardService leaderboards,
    IScoreService scores) : IUserDataService
{
    public ValueTask<bool> AddProfileAsync(Profile profile) => profiles.AddProfileAsync(profile);
    public ValueTask<Guid> GenerateAddProfileAsync(Profile profile) => profiles.GenerateAddProfileAsync(profile);
    public ValueTask<Profile?> GetProfileByIdAsync(Guid id) => profiles.GetProfileByIdAsync(id);
    public ValueTask<IReadOnlyDictionary<Guid, Profile>> GetProfilesByIdsAsync(IReadOnlyCollection<Guid> ids)
        => profiles.GetProfilesByIdsAsync(ids);
    public ValueTask<Profile?> GetProfileByTicketAsync(string ticket) => profiles.GetProfileByTicketAsync(ticket);
    public ValueTask<bool> HasJustDanceProfileAsync(Guid id) => profiles.HasJustDanceProfileAsync(id);
    public ValueTask<bool> UpdateProfileAsync(Profile profile) => profiles.UpdateProfileAsync(profile);

    public ValueTask<Leaderboard> GetLeaderboardAroundAsync(string mapName, Guid userId, int limit = 3)
        => leaderboards.GetLeaderboardAroundAsync(mapName, userId, limit);
    public ValueTask<Leaderboard> GetLeaderboardAsync(string mapName, long limit, long offset)
        => leaderboards.GetLeaderboardAsync(mapName, limit, offset);
    public ValueTask<Leaderboard> GetLeaderboardFromIdsAsync(string mapName, IReadOnlyCollection<Guid> userIds, long limit = 100)
        => leaderboards.GetLeaderboardFromIdsAsync(mapName, userIds, limit);
    public ValueTask<Leaderboard> GetCountryLeaderboardAsync(string mapName, int countryId, long limit)
        => leaderboards.GetCountryLeaderboardAsync(mapName, countryId, limit);
    public ValueTask<LeaderboardPage> GetDetailedLeaderboardAsync(
        string mapName,
        long limit,
        int? countryId = null,
        IReadOnlyCollection<Guid>? profileIds = null)
        => leaderboards.GetDetailedLeaderboardAsync(mapName, limit, countryId, profileIds);
    public ValueTask<Leaderboard> GetPlaylistLeaderboardAroundAsync(Guid playlistId, Guid userId, int limit = 3)
        => leaderboards.GetPlaylistLeaderboardAroundAsync(playlistId, userId, limit);
    public ValueTask<Leaderboard> GetPlaylistLeaderboardByPlaylistIdAsync(Guid playlistId, long limit, long offset)
        => leaderboards.GetPlaylistLeaderboardByPlaylistIdAsync(playlistId, limit, offset);
    public ValueTask<Leaderboard> GetPlaylistLeaderboardFromIdsAsync(Guid playlistId, List<Guid> userIds)
        => leaderboards.GetPlaylistLeaderboardFromIdsAsync(playlistId, userIds);
    public ValueTask<Leaderboard> GetRandomOpponentAsync(string mapName, Guid playerId)
        => leaderboards.GetRandomOpponentAsync(mapName, playerId);

    public ValueTask<MapStats?> GetHighScoreByProfileIdAndMapNameAsync(Guid profileId, string mapName)
        => scores.GetHighScoreByProfileIdAndMapNameAsync(profileId, mapName);
    public ValueTask<List<MapStats>> GetHighScoresByProfileIdAsync(Guid profileId)
        => scores.GetHighScoresByProfileIdAsync(profileId);
    public ValueTask<PlaylistStats?> GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(Guid profileId, Guid playlistId)
        => scores.GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(profileId, playlistId);
    public ValueTask<List<PlaylistStats>> GetPlaylistHighScoresByProfileIdAsync(Guid profileId)
        => scores.GetPlaylistHighScoresByProfileIdAsync(profileId);
    public ValueTask<ScoreUpdateResult> RecordMapPlayAsync(
        HighscorePerformance highScore,
        int score,
        Guid playerId,
        string mapName,
        string platform,
        bool isCoopHighScore = false)
        => scores.RecordMapPlayAsync(highScore, score, playerId, mapName, platform, isCoopHighScore);
    public ValueTask<ScoreUpdateResult> MergeMapScoreAsync(
        HighscorePerformance highScore,
        int score,
        int playCount,
        Guid playerId,
        string mapName,
        string platform,
        bool isCoopHighScore = false)
        => scores.MergeMapScoreAsync(
            highScore,
            score,
            playCount,
            playerId,
            mapName,
            platform,
            isCoopHighScore);
    public ValueTask<ScoreUpdateResult> UpdatePlaylistHighScoreAsync(
        PlaylistStats playlistStats,
        int score,
        Guid playerId,
        Guid playlistId) => scores.UpdatePlaylistHighScoreAsync(playlistStats, score, playerId, playlistId);
}
