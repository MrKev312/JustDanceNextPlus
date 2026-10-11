using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace JustDanceServer.Services;

public interface IPlayerActivityService
{
    ValueTask<bool> RecordJduHistoryAsync(PlayerRequestContext context, IReadOnlyList<JduMapEndedEntry> entries);
    ValueTask RecordJdNextObservationAsync(Guid profileId, string mapName, int score, string platform, bool cooperative);
    ValueTask<bool> RecordChallengeAsync(Guid matchId, Guid profileId, string mapName, int score, string platform);
    ValueTask StoreOrdinalsAsync(Guid profileId, string sourceScope, IReadOnlyDictionary<string, long> ordinals);
    ValueTask<PlayerMapPlay?> GetWeeklyWinnerAsync(string mapName);
}

public sealed class PlayerActivityService(
    UserDataContext dbContext,
    IMapService mapService,
    IScoreService scoreService,
    TimeProvider timeProvider) : IPlayerActivityService
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(90);
    public static readonly TimeSpan WeeklyWindow = TimeSpan.FromDays(7);
    private static readonly TimeSpan MaximumClockSkew = TimeSpan.FromMinutes(5);

    public async ValueTask<bool> RecordJduHistoryAsync(
        PlayerRequestContext context,
        IReadOnlyList<JduMapEndedEntry> entries)
    {
        if (context.ProfileId is not Guid profileId || profileId == Guid.Empty
            || !context.SupportsMapHistory || entries is null || entries.Count > 30)
            return false;
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<PlayerMapPlay> plays = [];
        Dictionary<string, int> occurrences = [with(StringComparer.Ordinal)];
        foreach (JduMapEndedEntry entry in entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.MapName) || entry.GameMode is null)
                return false;
            ServerMap? map = mapService.GetMap(entry.MapName);
            if (map is null || entry.Score is < 0 or > 13333
                || entry.NbPlayers is < 0 or > 6 || entry.Position is < 0 or > 6
                || entry.GameMode.Length > 32
                || entry.Timestamp < (now - Retention).ToUnixTimeSeconds()
                || entry.Timestamp > (now + MaximumClockSkew).ToUnixTimeSeconds())
                return false;
            if (entry.Score == 0)
                continue;

            string fingerprint = StableContentId.Hash(FormattableString.Invariant(
                $"{context.GameYear}|{context.Platform}|{map.Song.MapName}|{entry.Timestamp}|{entry.Score}|{entry.GameMode}|{entry.NbPlayers}|{entry.Position}|{entry.Wdf}|{entry.Coop}|{entry.Kids}|{entry.Playlist}|{entry.SweatEnabled}"));
            int occurrence = occurrences.GetValueOrDefault(fingerprint);
            occurrences[fingerprint] = occurrence + 1;
            plays.Add(new PlayerMapPlay
            {
                Id = Guid.NewGuid(),
                ProfileId = profileId,
                MapName = map.Song.MapName,
                Source = PlayerActivitySource.JduMapEnded,
                SourceEventKey = $"{fingerprint}:{occurrence.ToString(CultureInfo.InvariantCulture)}",
                Family = ProgressionFamily.JDUnlimited,
                GameYear = context.GameYear,
                Platform = context.Platform,
                PlayedAtUtc = DateTimeOffset.FromUnixTimeSeconds(entry.Timestamp).UtcDateTime,
                ReceivedAtUtc = now.UtcDateTime,
                Score = entry.Score,
                Mode = entry.Wdf ? "wdf" : entry.Coop ? "coop" : entry.Kids ? "kids" : "classic",
                Attribution = entry.Coop ? ScoreAttribution.Cooperative : ScoreAttribution.SessionBest,
                Completed = null,
                WeeklyEligible = false
            });
        }

        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync() : null;
        foreach (PlayerMapPlay play in plays)
            await InsertAsync(play);
        await PruneAsync(now.UtcDateTime);
        if (transaction is not null)
            await transaction.CommitAsync();
        return true;
    }

    public async ValueTask RecordJdNextObservationAsync(
        Guid profileId, string mapName, int score, string platform, bool cooperative)
    {
        if (score is <= 0 or > 13333 || profileId == Guid.Empty)
            return;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        // These requests have no established occurrence timestamp or play ID. They
        // inform discovery, but cannot become exactly-once competitive performances.
        await InsertAsync(new PlayerMapPlay
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            MapName = mapName,
            Source = PlayerActivitySource.JdNextMapPlayed,
            SourceEventKey = Guid.NewGuid().ToString("N"),
            Family = ProgressionFamily.JDPlus,
            Platform = platform,
            PlayedAtUtc = now,
            ReceivedAtUtc = now,
            Score = score,
            Mode = cooperative ? "coop" : "classic",
            Attribution = cooperative ? ScoreAttribution.Cooperative : ScoreAttribution.Unknown
        });
        await PruneAsync(now);
    }

    public async ValueTask<bool> RecordChallengeAsync(
        Guid matchId, Guid profileId, string mapName, int score, string platform)
    {
        if (matchId == Guid.Empty || profileId == Guid.Empty || score is < 0 or > 13333)
            return false;
        if (score == 0)
            return true;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync() : null;
        bool inserted = await InsertAsync(new PlayerMapPlay
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            MapName = mapName,
            Source = PlayerActivitySource.Challenge,
            SourceEventKey = matchId.ToString("N"),
            Family = ProgressionFamily.JDUnlimited,
            Platform = platform,
            PlayedAtUtc = now,
            ReceivedAtUtc = now,
            Score = score,
            Mode = "classic",
            Attribution = ScoreAttribution.Individual,
            Completed = true,
            WeeklyEligible = true
        });
        if (inserted)
        {
            ScoreUpdateResult result = await scoreService.RecordMapPlayAsync(
                new HighscorePerformance(), score, profileId, mapName, platform);
            if (!result.Success)
            {
                if (transaction is not null)
                    await transaction.RollbackAsync();
                else
                {
                    PlayerMapPlay stored = await dbContext.PlayerMapPlays.SingleAsync(value => value.ProfileId == profileId
                        && value.Source == PlayerActivitySource.Challenge && value.SourceEventKey == matchId.ToString("N"));
                    dbContext.PlayerMapPlays.Remove(stored);
                    await dbContext.SaveChangesAsync();
                }

                return false;
            }
        }

        await PruneAsync(now);
        if (transaction is not null)
            await transaction.CommitAsync();
        return true;
    }

    public async ValueTask StoreOrdinalsAsync(
        Guid profileId, string sourceScope, IReadOnlyDictionary<string, long> ordinals)
    {
        if (string.IsNullOrWhiteSpace(sourceScope) || sourceScope.Length > 128 || ordinals.Count > 10000)
            return;
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync() : null;
        foreach ((string name, long ordinal) in ordinals)
        {
            ServerMap? map = mapService.GetMap(name);
            if (ordinal <= 0 || map is null)
                continue;
            string mapName = map.Song.MapName;
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
					INSERT INTO "PlayerMapOrdinals" ("ProfileId", "SourceScope", "MapName", "Ordinal", "ObservedAtUtc")
					VALUES ({profileId}, {sourceScope}, {mapName}, {ordinal}, {now})
					ON CONFLICT ("ProfileId", "SourceScope", "MapName") DO UPDATE
					SET "Ordinal" = MAX("Ordinal", excluded."Ordinal"), "ObservedAtUtc" = excluded."ObservedAtUtc";
					""");
            }
            else
            {
                PlayerMapOrdinal? stored = await dbContext.PlayerMapOrdinals.FindAsync(profileId, sourceScope, mapName);
                if (stored is null)
                    dbContext.PlayerMapOrdinals.Add(new PlayerMapOrdinal
                    {
                        ProfileId = profileId,
                        SourceScope = sourceScope,
                        MapName = mapName,
                        Ordinal = ordinal,
                        ObservedAtUtc = now
                    });
                else
                {
                    stored.Ordinal = Math.Max(stored.Ordinal, ordinal);
                    stored.ObservedAtUtc = now;
                }
            }
        }

        if (!dbContext.Database.IsSqlite())
            await dbContext.SaveChangesAsync();
        await PruneAsync(now);
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async ValueTask<PlayerMapPlay?> GetWeeklyWinnerAsync(string mapName)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime cutoff = now - WeeklyWindow;
        return await dbContext.PlayerMapPlays.AsNoTracking()
            .Where(value => value.MapName == mapName && value.WeeklyEligible
                && value.PlayedAtUtc > cutoff && value.PlayedAtUtc <= now)
            .OrderByDescending(value => value.Score).ThenBy(value => value.PlayedAtUtc)
            .ThenBy(value => value.ProfileId).FirstOrDefaultAsync();
    }

    private async ValueTask<bool> InsertAsync(PlayerMapPlay play)
    {
        if (dbContext.Database.IsSqlite())
        {
            return await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
				INSERT INTO "PlayerMapPlays" ("Id", "ProfileId", "MapName", "Source", "SourceEventKey", "Family",
					"GameYear", "Platform", "PlayedAtUtc", "ReceivedAtUtc", "Score", "Mode", "Attribution", "Completed", "WeeklyEligible")
				VALUES ({play.Id}, {play.ProfileId}, {play.MapName}, {(int)play.Source}, {play.SourceEventKey}, {(int)play.Family},
					{play.GameYear}, {play.Platform}, {play.PlayedAtUtc}, {play.ReceivedAtUtc}, {play.Score}, {play.Mode},
					{(int)play.Attribution}, {play.Completed}, {play.WeeklyEligible})
				ON CONFLICT ("ProfileId", "Source", "SourceEventKey") DO NOTHING;
				""") > 0;
        }

        if (await dbContext.PlayerMapPlays.AnyAsync(value => value.ProfileId == play.ProfileId
            && value.Source == play.Source && value.SourceEventKey == play.SourceEventKey))
            return false;
        dbContext.PlayerMapPlays.Add(play);
        await dbContext.SaveChangesAsync();
        return true;
    }

    private async ValueTask PruneAsync(DateTime now)
    {
        DateTime cutoff = now - Retention;
        IQueryable<PlayerMapPlay> expired = dbContext.PlayerMapPlays.Where(value => value.PlayedAtUtc < cutoff);
        IQueryable<PlayerMapOrdinal> expiredOrdinals = dbContext.PlayerMapOrdinals.Where(value => value.ObservedAtUtc < cutoff);
        if (dbContext.Database.IsSqlite())
        {
            await expired.ExecuteDeleteAsync();
            await expiredOrdinals.ExecuteDeleteAsync();
        }
        else
        {
            dbContext.PlayerMapPlays.RemoveRange(await expired.ToListAsync());
            dbContext.PlayerMapOrdinals.RemoveRange(await expiredOrdinals.ToListAsync());
            await dbContext.SaveChangesAsync();
        }
    }
}

public static class StableContentId
{
    public static string Hash(string value)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static Guid Create(string value)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}
