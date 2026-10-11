using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.EntityFrameworkCore;

using System.Collections.Immutable;

namespace JustDanceServer.Services;

public sealed class LeaderboardService(UserDataContext dbContext) : ILeaderboardService
{
    public ValueTask<Leaderboard> GetLeaderboardAsync(string mapName, long limit, long offset)
        => GetPageAsync(GetMapRows(mapName), limit, offset);

    public ValueTask<Leaderboard> GetLeaderboardAroundAsync(string mapName, Guid userId, int limit = 3)
        => GetAroundAsync(GetMapRows(mapName), userId, limit);

    public ValueTask<Leaderboard> GetLeaderboardFromIdsAsync(
        string mapName,
        IReadOnlyCollection<Guid> userIds,
        long limit = 100)
        => GetFilteredAsync(GetMapRows(mapName), userIds, limit);

    public ValueTask<Leaderboard> GetCountryLeaderboardAsync(string mapName, int countryId, long limit)
        => GetPageAsync(GetCountryMapRows(mapName, countryId), limit, 0);

    public async ValueTask<LeaderboardPage> GetDetailedLeaderboardAsync(
        string mapName,
        long limit,
        int? countryId = null,
        IReadOnlyCollection<Guid>? profileIds = null)
    {
        Leaderboard leaderboard = countryId.HasValue
            ? await GetCountryLeaderboardAsync(mapName, countryId.Value, limit)
            : profileIds is not null
                ? await GetLeaderboardFromIdsAsync(mapName, profileIds, limit)
                : await GetLeaderboardAsync(mapName, limit, 0);

        if (leaderboard.Scores.Length == 0)
            return new LeaderboardPage([], leaderboard.Count);

        Guid[] ids = [.. leaderboard.Scores.Select(score => score.ProfileId)];
        Dictionary<Guid, DetailedProfile> details = await (
            from profile in dbContext.Profiles.AsNoTracking()
            where ids.Contains(profile.Id)
            join classic in dbContext.ClassicProfiles.AsNoTracking()
                on profile.Id equals classic.ProfileId into classicProfiles
            from classic in classicProfiles.DefaultIfEmpty()
            select new DetailedProfile(
                profile.Id,
                classic == null ? profile.Dancercard.Name : classic.Name,
                classic == null ? 0 : classic.CountryId,
                classic == null ? 0 : classic.AvatarId,
                classic == null ? 0 : classic.JdPoints,
                classic == null ? 0 : classic.AliasId,
                classic == null ? 0 : classic.AliasGender,
                classic == null ? 0 : classic.PortraitBorderId,
                classic == null ? profile.Id.ToString() : classic.PlatformId))
            .ToDictionaryAsync(profile => profile.ProfileId);

        List<LeaderboardPlayerScore> entries = [];
        foreach (ScoreDetails score in leaderboard.Scores)
        {
            if (!details.TryGetValue(score.ProfileId, out DetailedProfile? profile))
                continue;

            entries.Add(new LeaderboardPlayerScore(
                score.ProfileId,
                profile.Name,
                profile.CountryId,
                profile.AvatarId,
                score.Score,
                score.Position,
                profile.JdPoints,
                profile.AliasId,
                profile.AliasGender,
                profile.PortraitBorderId,
                string.IsNullOrWhiteSpace(profile.PlatformId)
                    ? profile.ProfileId.ToString()
                    : profile.PlatformId));
        }

        return new LeaderboardPage(entries, leaderboard.Count);
    }

    public async ValueTask<Leaderboard> GetRandomOpponentAsync(string mapName, Guid playerId)
    {
        IQueryable<LeaderboardRow> allRows = GetMapRows(mapName);
        IQueryable<LeaderboardRow> opponents = allRows.Where(row => row.ProfileId != playerId);
        long opponentCount = await opponents.LongCountAsync();

        if (opponentCount == 0)
        {
            opponents = allRows;
            opponentCount = await opponents.LongCountAsync();
        }

        if (opponentCount == 0)
            return new Leaderboard();

        int randomOffset = Random.Shared.Next((int)Math.Min(opponentCount, int.MaxValue));
        LeaderboardRow? opponent = await Order(opponents).Skip(randomOffset).FirstOrDefaultAsync();
        if (opponent is null)
            return new Leaderboard();

        long position = await allRows.LongCountAsync(row => row.Score > opponent.Score) + 1;
        return new Leaderboard
        {
            Count = 1,
            Scores =
            [
                new ScoreDetails
                {
                    ProfileId = opponent.ProfileId,
                    Score = opponent.Score,
                    Position = position
                }
            ]
        };
    }

    public ValueTask<Leaderboard> GetPlaylistLeaderboardByPlaylistIdAsync(
        Guid playlistId,
        long limit,
        long offset)
        => GetPageAsync(GetPlaylistRows(playlistId), limit, offset);

    public ValueTask<Leaderboard> GetPlaylistLeaderboardAroundAsync(
        Guid playlistId,
        Guid userId,
        int limit = 3)
        => GetAroundAsync(GetPlaylistRows(playlistId), userId, limit);

    public ValueTask<Leaderboard> GetPlaylistLeaderboardFromIdsAsync(
        Guid playlistId,
        List<Guid> userIds)
        => GetFilteredAsync(GetPlaylistRows(playlistId), userIds, 100);

    private static async ValueTask<Leaderboard> GetPageAsync(
        IQueryable<LeaderboardRow> allRows,
        long limit,
        long offset)
    {
        int boundedLimit = (int)Math.Clamp(limit, 0, 100);
        int boundedOffset = (int)Math.Clamp(offset, 0, int.MaxValue);
        long playerCount = await allRows.LongCountAsync();
        if (boundedLimit == 0 || playerCount == 0)
            return new Leaderboard { Count = playerCount };

        List<LeaderboardRow> rows = await Order(allRows)
            .Skip(boundedOffset)
            .Take(boundedLimit)
            .ToListAsync();

        long firstPosition = rows.Count == 0
            ? 0
            : await GetPositionAsync(allRows, rows[0].Score);
        return CreateLeaderboard(rows, playerCount, boundedOffset, firstPosition);
    }

    private static async ValueTask<Leaderboard> GetAroundAsync(
        IQueryable<LeaderboardRow> allRows,
        Guid userId,
        int limit)
    {
        int boundedLimit = Math.Clamp(limit, 1, 100);
        long playerCount = await allRows.LongCountAsync();
        if (playerCount == 0)
            return new Leaderboard();

        LeaderboardRow? user = await allRows.FirstOrDefaultAsync(row => row.ProfileId == userId);
        if (user is null)
            return await GetPageAsync(allRows, boundedLimit, 0);

        long rowsBeforeUser = await allRows.LongCountAsync(row => row.Score > user.Score);
        // Keep tie ordering in the database: SQLite TEXT GUID ordering differs from .NET.
        List<Guid> tiedIds = await Order(allRows.Where(row => row.Score == user.Score))
            .Select(row => row.ProfileId).ToListAsync();
        rowsBeforeUser += Math.Max(0, tiedIds.IndexOf(userId));
        long offset = Math.Max(0, rowsBeforeUser - (boundedLimit / 2));

        List<LeaderboardRow> rows = await Order(allRows)
            .Skip((int)offset)
            .Take(boundedLimit)
            .ToListAsync();

        long firstPosition = rows.Count == 0
            ? 0
            : await GetPositionAsync(allRows, rows[0].Score);
        return CreateLeaderboard(rows, playerCount, offset, firstPosition);
    }

    private static async ValueTask<Leaderboard> GetFilteredAsync(
        IQueryable<LeaderboardRow> allRows,
        IReadOnlyCollection<Guid> userIds,
        long limit)
    {
        if (userIds.Count == 0)
            return new Leaderboard();

        Guid[] ids = [.. userIds.Distinct()];
        IQueryable<LeaderboardRow> filteredRows = allRows.Where(row => ids.Contains(row.ProfileId));
        long playerCount = await filteredRows.LongCountAsync();
        List<LeaderboardRow> rows = await Order(filteredRows)
            .Take((int)Math.Clamp(limit, 0, 100))
            .ToListAsync();

        return CreateLeaderboard(rows, playerCount, 0, rows.Count == 0 ? 0 : 1);
    }

    private static async ValueTask<long> GetPositionAsync(
        IQueryable<LeaderboardRow> allRows,
        int score)
        => await allRows.LongCountAsync(row => row.Score > score) + 1;

    private IQueryable<LeaderboardRow> GetMapRows(string mapName)
        => from score in dbContext.MapScores.AsNoTracking()
           where score.MapName == mapName
           join profile in dbContext.Profiles.AsNoTracking() on score.ProfileId equals profile.Id
           select new LeaderboardRow
           {
               ProfileId = score.ProfileId,
               Score = score.HighScore,
               ProfileName = profile.Dancercard.Name,
               UpdatedAt = score.UpdatedAt
           };

    private IQueryable<LeaderboardRow> GetCountryMapRows(string mapName, int countryId)
        => from score in dbContext.MapScores.AsNoTracking()
           where score.MapName == mapName
           join classicProfile in dbContext.ClassicProfiles.AsNoTracking()
               on score.ProfileId equals classicProfile.ProfileId
           where classicProfile.CountryId == countryId
           select new LeaderboardRow
           {
               ProfileId = score.ProfileId,
               Score = score.HighScore,
               ProfileName = classicProfile.Name,
               UpdatedAt = score.UpdatedAt
           };

    private IQueryable<LeaderboardRow> GetPlaylistRows(Guid playlistId)
        => from score in dbContext.PlaylistScores.AsNoTracking()
           where score.PlaylistId == playlistId
           join profile in dbContext.Profiles.AsNoTracking() on score.ProfileId equals profile.Id
           select new LeaderboardRow
           {
               ProfileId = score.ProfileId,
               Score = score.HighScore,
               ProfileName = profile.Dancercard.Name,
               UpdatedAt = score.UpdatedAt
           };

    private static IOrderedQueryable<LeaderboardRow> Order(IQueryable<LeaderboardRow> rows)
        => rows.OrderByDescending(row => row.Score)
            .ThenBy(row => row.ProfileName)
            .ThenBy(row => row.ProfileId);

    private static Leaderboard CreateLeaderboard(
        IReadOnlyList<LeaderboardRow> rows,
        long playerCount,
        long offset,
        long firstPosition)
    {
        if (rows.Count == 0)
            return new Leaderboard { Count = playerCount };

        ImmutableArray<ScoreDetails>.Builder scores = ImmutableArray.CreateBuilder<ScoreDetails>(rows.Count);
        long position = firstPosition;
        int previousScore = rows[0].Score;
        for (int index = 0; index < rows.Count; index++)
        {
            LeaderboardRow row = rows[index];
            if (row.Score != previousScore)
            {
                position = offset + index + 1;
                previousScore = row.Score;
            }

            scores.Add(new ScoreDetails
            {
                ProfileId = row.ProfileId,
                Score = row.Score,
                Position = position
            });
        }

        return new Leaderboard { Count = playerCount, Scores = scores.ToImmutable() };
    }

    private sealed class LeaderboardRow
    {
        public Guid ProfileId { get; init; }
        public int Score { get; init; }
        public string ProfileName { get; init; } = string.Empty;
        public DateTime UpdatedAt { get; init; }
    }

    private sealed record DetailedProfile(
        Guid ProfileId,
        string Name,
        int CountryId,
        uint AvatarId,
        int JdPoints,
        uint AliasId,
        int AliasGender,
        uint PortraitBorderId,
        string PlatformId);
}
