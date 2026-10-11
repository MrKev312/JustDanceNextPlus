using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JustDanceServer.Tests.Services;

public sealed class ScoringIntegrationTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(
        Path.GetTempPath(),
        $"jdnp-scoring-{Guid.NewGuid():N}");

    [Fact]
    public async Task ConcurrentSubmissionsAtomicallyKeepHighestScoreAndEveryPlay()
    {
        Directory.CreateDirectory(tempDirectory);
        string databasePath = Path.Combine(tempDirectory, "scores.db");
        string connectionString = $"Data Source={databasePath};Pooling=False;Default Timeout=30";
        DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
            .UseSqlite(connectionString)
            .Options;

        await using (UserDataContext setup = new(options))
        {
            await setup.Database.EnsureCreatedAsync();
            await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await setup.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");
        }

        Guid profileId = Guid.NewGuid();
        int[] submittedScores = [9000, 10000, 11000, 12000, 13000, 12500, 9500, 13333];
        Task<ScoreUpdateResult>[] writes = [.. submittedScores.Select(async score =>
        {
            await using UserDataContext context = new(options);
            ScoreService service = new(context, NullLogger<ScoreService>.Instance);
            return await service.RecordMapPlayAsync(
                new HighscorePerformance(),
                score,
                profileId,
                "PokerFaceALT",
                "switch");
        })];

        ScoreUpdateResult[] results = await Task.WhenAll(writes);
        Assert.All(results, result => Assert.True(result.Success));

        await using UserDataContext verify = new(options);
        MapStats stored = await verify.MapScores.SingleAsync();
        Assert.Equal("PokerFaceALT", stored.MapName);
        Assert.Equal(13333, stored.HighScore);
        Assert.Equal(submittedScores.Length, stored.PlayCount);
    }

    [Fact]
    public async Task DetailedLeaderboardProjectsJduFieldsFromSharedJdPlusScores()
    {
        await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync();
        DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
            .UseSqlite(connection)
            .Options;
        await using UserDataContext context = new(options);
        await context.Database.EnsureCreatedAsync();

        Profile first = CreateProfile("JDPlus Player");
        Profile second = CreateProfile("Second Player");
        context.Profiles.AddRange(first, second);
        context.ClassicProfiles.AddRange(
            new ClassicProfile
            {
                ProfileId = first.Id,
                Name = "JDU Player",
                CountryId = 33,
                AvatarId = 4,
                JdPoints = 500,
                AliasId = 6,
                AliasGender = 1,
                PortraitBorderId = 7,
                PlatformId = "wiiu-player"
            },
            new ClassicProfile
            {
                ProfileId = second.Id,
                Name = "Other JDU Player",
                CountryId = 1,
                PlatformId = "switch-player"
            });
        context.MapScores.AddRange(
            new MapStats { MapName = "PokerFaceALT", ProfileId = first.Id, HighScore = 13333 },
            new MapStats { MapName = "PokerFaceALT", ProfileId = second.Id, HighScore = 12000 });
        await context.SaveChangesAsync();

        LeaderboardService service = new(context);
        LeaderboardPage world = await service.GetDetailedLeaderboardAsync("PokerFaceALT", 5);
        LeaderboardPage country = await service.GetDetailedLeaderboardAsync("PokerFaceALT", 5, countryId: 33);

        Assert.Equal(2, world.PlayerCount);
        LeaderboardPlayerScore winner = Assert.Single(world.Entries, entry => entry.ProfileId == first.Id);
        Assert.Equal(1, winner.Rank);
        Assert.Equal("JDU Player", winner.Name);
        Assert.Equal(33, winner.CountryId);
        Assert.Equal(4u, winner.AvatarId);
        Assert.Equal("wiiu-player", winner.PlatformId);
        Assert.Single(country.Entries);
        Assert.Equal(first.Id, country.Entries[0].ProfileId);
    }

    [Fact]
    public async Task MapHighScoreAlwaysStoresServerOwnedAchievementTime()
    {
        await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync();
        DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
            .UseSqlite(connection)
            .Options;
        await using UserDataContext context = new(options);
        await context.Database.EnsureCreatedAsync();

        Guid profileId = Guid.NewGuid();
        DateTimeOffset firstAchievement = new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        TestTimeProvider serverClock = new(firstAchievement);
        ScoreService service = new(context, NullLogger<ScoreService>.Instance, serverClock);

        Assert.True((await service.RecordMapPlayAsync(
            new HighscorePerformance(),
            10_000,
            profileId,
            "PokerFaceALT",
            "switch")).Success);
        context.ChangeTracker.Clear();
        Assert.Equal(firstAchievement.UtcDateTime, (await context.MapScores.SingleAsync()).UpdatedAt);

        serverClock.UtcNow = firstAchievement.AddDays(1);
        Assert.True((await service.RecordMapPlayAsync(
            new HighscorePerformance(),
            9_000,
            profileId,
            "PokerFaceALT",
            "switch")).Success);
        context.ChangeTracker.Clear();
        Assert.Equal(firstAchievement.UtcDateTime, (await context.MapScores.SingleAsync()).UpdatedAt);

        DateTimeOffset secondAchievement = firstAchievement.AddDays(2);
        serverClock.UtcNow = secondAchievement;
        Assert.True((await service.RecordMapPlayAsync(
            new HighscorePerformance(),
            11_000,
            profileId,
            "PokerFaceALT",
            "switch")).Success);
        context.ChangeTracker.Clear();
        Assert.Equal(secondAchievement.UtcDateTime, (await context.MapScores.SingleAsync()).UpdatedAt);
    }

    private static Profile CreateProfile(string name)
    {
        Guid id = Guid.NewGuid();
        return new Profile
        {
            Id = id,
            Ticket = $"ticket-{id:N}",
            Dancercard = new DancerCard { Id = id, Name = name }
        };
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
            Directory.Delete(tempDirectory, recursive: true);
    }
}
