using System.Data.Common;
using System.Text.Json;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.GraphQL;
using JustDanceServer.JustDanceClasses.GraphQL.Objects;
using JustDanceServer.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JustDanceServer.Tests.Services;

public sealed class ProfileBatchTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("not-a-guid")]
	public async Task LeaderboardQuery_InvalidOptionalIdsKeepDefaultProjections(string? id)
	{
		Profile profile = CreateProfile("Player");
		Mock<IUserDataService> data = new();
		data.Setup(service => service.GetProfilesByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>()))
			.ReturnsAsync(new Dictionary<Guid, Profile> { [profile.Id] = profile });
		Mock<IMapService> maps = new();
		Query query = new();

		List<GraphQlLeaderboardProfile> result = await query.GetProfilesLeaderboardById(
			[profile.Id.ToString()], id, id, data.Object, maps.Object);

		GraphQlLeaderboardProfile player = Assert.Single(result);
		Assert.Equal(profile.Id, player.Id);
		Assert.Equal("uplay", player.MapStat.Platform);
		Assert.Equal("uplay", player.PlaylistStat.Platform);
		maps.Verify(service => service.TryGetMapName(Guid.Empty, out It.Ref<string>.IsAny), Times.Once);
	}

    [Fact]
    public async Task BatchReadsPreserveOrderDuplicatesMissingIdsAndProfileProjections()
    {
        await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync();
        CommandCounter counter = new();
        await using UserDataContext db = new(new DbContextOptionsBuilder<UserDataContext>()
            .UseSqlite(connection).AddInterceptors(counter).Options);
        await db.Database.EnsureCreatedAsync();
        Profile[] players = [CreateProfile("A"), CreateProfile("B"), CreateProfile("C")];
        Guid playlist = Guid.NewGuid();
        db.Profiles.AddRange(players);
        db.MapScores.Add(new() { ProfileId = players[1].Id, MapName = "Song", HighScore = 12000 });
        db.PlaylistScores.Add(new() { ProfileId = players[1].Id, PlaylistId = playlist, HighScore = 12000 });
        db.PlayerProgressions.Add(new() { ProfileId = players[1].Id, Family = ProgressionFamily.JDPlus, Xp = 555, Level = 4 });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        UserProfileService profiles = new(db, NullLogger<UserProfileService>.Instance,
            progressionService: new ProgressionService(db));
        UserDataService data = new(profiles, new LeaderboardService(db), new ScoreService(db, NullLogger<ScoreService>.Instance));
        Guid[] order = [players[2].Id, players[1].Id, players[1].Id, players[0].Id];
        List<string> ids = [order[0].ToString(), "invalid", Guid.NewGuid().ToString(), .. order.Skip(1).Select(id => id.ToString())];
        List<Profile> expected = [];
        foreach (Guid id in order)
			expected.Add((await profiles.GetProfileByIdAsync(id))!);
        counter.Count = 0;
		IReadOnlyDictionary<Guid, Profile> batch = await profiles.GetProfilesByIdsAsync([.. order]);
        Assert.Equal(4, counter.Count);
        Assert.Equal(555, batch[players[1].Id].CurrentXP);
        Assert.Equal(4, batch[players[1].Id].CurrentLevel);
        Assert.Equal(12000, batch[players[1].Id].PlaylistStats[playlist].HighScore);

        Query query = new();
		List<GraphQlProfile> result = await query.GetProfilesById(ids, data);
        Assert.Equal(JsonSerializer.Serialize(expected.Select(GraphQlProfile.FromProfile)), JsonSerializer.Serialize(result));
        Guid mapId = Guid.NewGuid();
        string mapName = "Song";
        Mock<IMapService> maps = new();
        maps.Setup(value => value.TryGetMapName(mapId, out mapName)).Returns(true);
        counter.Count = 0;
		List<GraphQlLeaderboardProfile> leaderboard = await query.GetProfilesLeaderboardById(ids, mapId.ToString(), playlist.ToString(), data, maps.Object);
        Assert.Equal(4, counter.Count);
        Assert.Equal(JsonSerializer.Serialize(expected.Select(profile => GraphQlLeaderboardProfile.FromProfile(profile, mapName, playlist))),
            JsonSerializer.Serialize(leaderboard));
	}

	private static Profile CreateProfile(string name)
    {
        Guid id = Guid.NewGuid();
        return new() { Id = id, Ticket = id.ToString(), Dancercard = new() { Id = id, Name = name } };
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return ValueTask.FromResult(result);
        }
    }
}
