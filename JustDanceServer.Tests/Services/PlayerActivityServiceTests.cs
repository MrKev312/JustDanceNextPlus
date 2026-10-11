using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace JustDanceServer.Tests.Services;

public sealed class PlayerActivityServiceTests
{
    [Theory]
    [InlineData(2019)]
    [InlineData(2020)]
    [InlineData(2021)]
    [InlineData(2022)]
    public async Task BatchedHistoryKeepsOriginalTimestampAndSuppressesRegroupedRetries(int year)
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        long timestamp = test.Clock.Now.AddDays(-2).ToUnixTimeSeconds();
        JduMapEndedEntry first = new() { MapName = "Alpha", Score = 11000, Timestamp = timestamp, NbPlayers = 2 };
        JduMapEndedEntry second = first with { MapName = "Beta", Coop = true, Score = 12500 };

        Assert.True(await test.Activity.RecordJduHistoryAsync(test.Request(year), [first, second, first]));
        test.Clock.Now = test.Clock.Now.AddHours(1);
        Assert.True(await test.Activity.RecordJduHistoryAsync(test.Request(year), [second, first, first]));
        Assert.True(await test.Activity.RecordJduHistoryAsync(test.Request(year), [first]));

        PlayerMapPlay[] plays = await test.Db.PlayerMapPlays.AsNoTracking().ToArrayAsync();
        Assert.Equal(3, plays.Length);
        Assert.Equal(2, plays.Count(value => value.MapName == "Alpha"));
        Assert.All(plays, play =>
        {
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime, play.PlayedAtUtc);
            Assert.Equal(year, play.GameYear);
            Assert.False(play.WeeklyEligible);
            Assert.Null(play.Completed);
            Assert.True(play.ReceivedAtUtc > play.PlayedAtUtc);
        });
        Assert.Equal(ScoreAttribution.Cooperative, plays.Single(value => value.MapName == "Beta").Attribution);
        Assert.All(plays.Where(value => value.MapName == "Alpha"), play => Assert.Equal(ScoreAttribution.SessionBest, play.Attribution));
        Assert.Empty(await test.Db.MapScores.ToListAsync());
        Assert.Empty(await test.Db.PlayerProgressions.ToListAsync());
        Assert.Null(await test.Activity.GetWeeklyWinnerAsync("Alpha"));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("old")]
    [InlineData("future")]
    [InlineData("score")]
    [InlineData("null")]
    [InlineData("mode")]
    [InlineData("count")]
    public async Task InvalidBatchHasNoPartialWrites(string invalid)
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        JduMapEndedEntry valid = new() { MapName = "Alpha", Timestamp = test.Clock.Now.ToUnixTimeSeconds(), Score = 10000 };
        JduMapEndedEntry bad = invalid switch
        {
            "unknown" => valid with { MapName = "Missing" },
            "old" => valid with { Timestamp = test.Clock.Now.AddDays(-91).ToUnixTimeSeconds() },
            "future" => valid with { Timestamp = test.Clock.Now.AddMinutes(6).ToUnixTimeSeconds() },
            "score" => valid with { Score = 13334 },
            "null" => null!,
            "mode" => valid with { GameMode = null! },
            _ => valid
        };
        JduMapEndedEntry[] batch = invalid == "count" ? [.. Enumerable.Repeat(valid, 31)] : [valid, bad];
        Assert.False(await test.Activity.RecordJduHistoryAsync(test.Request(), batch));
        Assert.Empty(await test.Db.PlayerMapPlays.ToListAsync());
    }

    [Fact]
    public async Task OrdinalsAreScopedMonotonicObservationsAndNeverDatedPerformances()
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        await test.Activity.StoreOrdinalsAsync(test.Player, "save-a", new Dictionary<string, long> { ["Alpha"] = 500, ["Beta"] = 499 });
        await test.Activity.StoreOrdinalsAsync(test.Player, "save-a", new Dictionary<string, long> { ["Alpha"] = 450 });
        await test.Activity.StoreOrdinalsAsync(test.Player, "save-b", new Dictionary<string, long> { ["Alpha"] = 1 });
        PlayerMapOrdinal[] ordinals = await test.Db.PlayerMapOrdinals.AsNoTracking().ToArrayAsync();
        Assert.Equal(3, ordinals.Length);
        Assert.Equal(500, ordinals.Single(value => value.SourceScope == "save-a" && value.MapName == "Alpha").Ordinal);
        Assert.Equal(1, ordinals.Single(value => value.SourceScope == "save-b").Ordinal);
        Assert.Empty(await test.Db.PlayerMapPlays.ToListAsync());
        Assert.Empty(await test.Recommendations.GetMapsAsync(new(RecommendationPolicy.Recent, test.Player)));

        test.Clock.Now = test.Clock.Now.AddDays(91);
        await test.Activity.StoreOrdinalsAsync(test.Player, "save-c", new Dictionary<string, long> { ["Gamma"] = 2 });
        Assert.Equal("save-c", (await test.Db.PlayerMapOrdinals.AsNoTracking().SingleAsync()).SourceScope);
    }

    [Fact]
    public async Task ChallengeReplayIsAtomicAndWeeklyScoreIsNotTheAllTimeBest()
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        await test.Scores.RecordMapPlayAsync(new(), 13000, test.Player, "Alpha", "switch");
        Guid matchId = Guid.NewGuid();
        Assert.True(await test.Activity.RecordChallengeAsync(matchId, test.Player, "Alpha", 11000, "switch"));
        Assert.True(await test.Activity.RecordChallengeAsync(matchId, test.Player, "Alpha", 11000, "switch"));
        PlayerMapPlay winner = Assert.IsType<PlayerMapPlay>(await test.Activity.GetWeeklyWinnerAsync("Alpha"));
        Assert.Equal(11000, winner.Score);
        Assert.True(winner.Completed);
        MapStats stats = await test.Db.MapScores.AsNoTracking().SingleAsync();
        Assert.Equal(13000, stats.HighScore);
        Assert.Equal(2, stats.PlayCount);
        Assert.Single(await test.Db.PlayerMapPlays.ToListAsync());

        test.Clock.Now = test.Clock.Now.AddDays(7);
        Assert.Null(await test.Activity.GetWeeklyWinnerAsync("Alpha"));
        Assert.Equal(13000, (await test.Db.MapScores.AsNoTracking().SingleAsync()).HighScore);
    }

    [Fact]
    public async Task RejectedChallengeRollsBackItsEligibilityAndCanBeRetried()
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        Mock<IScoreService> rejectedScores = new();
        rejectedScores.Setup(value => value.RecordMapPlayAsync(It.IsAny<HighscorePerformance>(), 11000,
            test.Player, "Alpha", "switch", false)).ReturnsAsync(new ScoreUpdateResult(false, false));
        PlayerActivityService activity = new(test.Db, test.Maps.Object, rejectedScores.Object, test.Clock);
        Guid matchId = Guid.NewGuid();
        Assert.False(await activity.RecordChallengeAsync(matchId, test.Player, "Alpha", 11000, "switch"));
        Assert.Empty(await test.Db.PlayerMapPlays.ToListAsync());
        Assert.True(await test.Activity.RecordChallengeAsync(matchId, test.Player, "Alpha", 11000, "switch"));
        Assert.Single(await test.Db.PlayerMapPlays.ToListAsync());
    }

    [Fact]
    public async Task WeeklyTiesPreferEarlierPerformanceAndIgnoreAmbiguousObservations()
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        await test.Activity.RecordChallengeAsync(Guid.NewGuid(), test.Player, "Alpha", 11000, "switch");
        test.Clock.Now = test.Clock.Now.AddSeconds(1);
        await test.Activity.RecordChallengeAsync(Guid.NewGuid(), test.OtherPlayer, "Alpha", 11000, "switch");
        await test.Activity.RecordJdNextObservationAsync(test.OtherPlayer, "Alpha", 13000, "switch", false);
        Assert.Equal(test.Player, (await test.Activity.GetWeeklyWinnerAsync("Alpha"))!.ProfileId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task JdNextCompletionPreservesXpPolicyAndRecordsOnlyAnObservation(bool grantXp)
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        MapCompletionService completions = new(test.Db, test.Scores, new ProgressionService(test.Db), test.Activity);
        MapCompletionResult result = await completions.RecordAsync(new(test.Player, "Alpha", 10000, "switch", false, new(), grantXp));
        Assert.True(result.Score.Success);
        Assert.Equal(grantXp, result.Progression is not null);
        PlayerMapPlay observation = await test.Db.PlayerMapPlays.SingleAsync();
        Assert.False(observation.WeeklyEligible);
        Assert.Equal(ScoreAttribution.Unknown, observation.Attribution);
        Assert.Equal(1, (await test.Db.MapScores.AsNoTracking().SingleAsync()).PlayCount);
    }

    [Fact]
    public async Task JdNextCompletionRollsBackScoreIfActivityPersistenceFails()
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        Mock<IPlayerActivityService> failing = new();
        failing.Setup(value => value.RecordJdNextObservationAsync(test.Player, "Alpha", 10000, "switch", false))
            .ThrowsAsync(new InvalidOperationException("Persistence failed"));
        MapCompletionService completions = new(test.Db, test.Scores, new ProgressionService(test.Db), failing.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await completions.RecordAsync(new(test.Player, "Alpha", 10000, "switch", false, new(), true)));
        Assert.Empty(await test.Db.MapScores.AsNoTracking().ToListAsync());
        Assert.Empty(await test.Db.PlayerProgressions.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task ZeroScoresAndExpiredObservationsDoNotPolluteHistory()
    {
        await using DiscoveryTestContext test = new();
        await test.InitializeAsync();
        await test.Activity.RecordChallengeAsync(Guid.NewGuid(), test.Player, "Alpha", 0, "switch");
        await test.Activity.RecordJdNextObservationAsync(test.Player, "Alpha", 0, "switch", false);
        Assert.True(await test.Activity.RecordJduHistoryAsync(test.Request(),
            [new() { MapName = "Alpha", Score = 0, Timestamp = test.Clock.Now.ToUnixTimeSeconds() }]));
        Assert.Empty(await test.Db.PlayerMapPlays.ToListAsync());
        await test.Activity.RecordJdNextObservationAsync(test.Player, "Alpha", 10000, "switch", false);
        test.Clock.Now = test.Clock.Now.AddDays(91);
        await test.Activity.RecordJdNextObservationAsync(test.Player, "Beta", 11000, "switch", false);
        Assert.Equal("Beta", (await test.Db.PlayerMapPlays.AsNoTracking().SingleAsync()).MapName);
    }
}
