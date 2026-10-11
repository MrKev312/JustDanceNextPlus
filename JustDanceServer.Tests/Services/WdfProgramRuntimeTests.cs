using JustDanceServer.Services;

namespace JustDanceServer.Tests.Services;

public sealed class WdfProgramRuntimeTests
{
    [Fact]
    public void BossDamageUsesOnlyEachPlayersIncreasingScore()
    {
        WdfProgramRuntime runtime = CreateRuntime(new WdfProgramDefinition
        {
            Id = "boss",
            Kind = WdfProgramKind.BossBattle,
            Boss = new WdfBossDefinition { Name = "Boss", Health = 20_000 }
        });
        Guid player = Guid.NewGuid();

        runtime.SubmitScore(player, 5_000);
        runtime.SubmitScore(player, 7_000);
        runtime.SubmitScore(player, 6_000);

        WdfBossRuntime boss = Assert.IsType<WdfBossRuntime>(runtime.Mode);
        Assert.Equal(13_000, boss.RemainingHealth);
        Assert.Equal(7_000, boss.Contributions[player]);
    }

    [Fact]
    public void TeamBattleBalancesNewPlayersAndAggregatesTheirScores()
    {
        WdfProgramRuntime runtime = CreateRuntime(new WdfProgramDefinition
        {
            Id = "teams",
            Kind = WdfProgramKind.TeamBattle,
            Teams =
            [
                new WdfTeamDefinition { Id = "red", Name = "Red" },
                new WdfTeamDefinition { Id = "blue", Name = "Blue" }
            ]
        });

        runtime.SubmitScore(Guid.NewGuid(), 1_000);
        runtime.SubmitScore(Guid.NewGuid(), 2_000);

        WdfTeamBattleRuntime teams = Assert.IsType<WdfTeamBattleRuntime>(runtime.Mode);
        Assert.Equal(2, teams.Assignments.Count);
        Assert.Equal(1, teams.Assignments.Values.Count(value => value == "red"));
        Assert.Equal(1, teams.Assignments.Values.Count(value => value == "blue"));
        Assert.Equal(3_000, teams.Scores.Values.Sum());
    }

    [Fact]
    public void TournamentEnforcesItsParticipantLimit()
    {
        WdfProgramRuntime runtime = CreateRuntime(new WdfProgramDefinition
        {
            Id = "tournament",
            Kind = WdfProgramKind.Tournament,
            Tournament = new WdfTournamentDefinition { MaximumParticipants = 2 }
        });

        runtime.SubmitScore(Guid.NewGuid(), 1_000);
        runtime.SubmitScore(Guid.NewGuid(), 2_000);
        runtime.SubmitScore(Guid.NewGuid(), 3_000);

        WdfTournamentRuntime tournament = Assert.IsType<WdfTournamentRuntime>(runtime.Mode);
        Assert.Equal(2, tournament.Scores.Count);
    }

    private static WdfProgramRuntime CreateRuntime(WdfProgramDefinition definition)
        => new(definition, WdfRoomId.HappyHour, DateTimeOffset.UnixEpoch);
}
