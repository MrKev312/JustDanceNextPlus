using System.Text.Json;
using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.GraphQL;
using JustDanceServer.JustDanceClasses.GraphQL.Objects;
using JustDanceServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MapController = JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile.map.Played;
using PlaylistController = JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile.playlist.Played;

namespace JustDanceServer.Tests.Services;

public sealed class ZeroScoreContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestAndGraphQlAcknowledgeZeroScoresWithoutChangingScoresCountsOrProgression(bool hasScore)
    {
        await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
        await connection.OpenAsync();
        await using UserDataContext db = new(new DbContextOptionsBuilder<UserDataContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        Guid player = Guid.NewGuid(), song = Guid.NewGuid(), playlist = Guid.NewGuid();
        const string mapName = "TestMap";
        db.Profiles.Add(new Profile { Id = player, Dancercard = new() { Id = player, Name = "Player" } });
        await db.SaveChangesAsync();
        ScoreService scores = new(db, NullLogger<ScoreService>.Instance);
        ProgressionService progression = new(db);
        if (hasScore)
        {
            await scores.RecordMapPlayAsync(new(), 12000, player, mapName, "switch");
            await scores.UpdatePlaylistHighScoreAsync(new() { HighScorePerMap = new() { [mapName] = 12000 } },
                12000, player, playlist);
            await progression.SetProgressionAsync(player, ProgressionFamily.JDPlus, 1500, 7, 2);
        }

        db.ChangeTracker.Clear();
        string before = await SnapshotAsync(db);
        UserDataService data = new(new UserProfileService(db, NullLogger<UserProfileService>.Instance,
            progressionService: progression), new LeaderboardService(db), scores);
        Mock<IMapService> maps = new();
        string resolvedName = mapName;
        maps.Setup(value => value.TryGetMapName(song, out resolvedName)).Returns(true);
        maps.Setup(value => value.GetSongId(mapName)).Returns(song);
        SessionManager sessions = new(new SecurityService());
        (string? ticket, Guid sessionId) = sessions.GenerateSession(player, JdNextConstants.SwitchApplicationId, "zero-score");
        try
        {
            DefaultHttpContext http = new();
            http.Request.Headers["ubi-sessionid"] = sessionId.ToString();
            HttpContextAccessor accessor = new() { HttpContext = http };
            Mock<IProgressionService> noProgressionWrites = new(MockBehavior.Strict);
			PlayerActivityService activity = new(db, maps.Object, scores, TimeProvider.System);
			MapCompletionService completions = new(db, scores, noProgressionWrites.Object, activity);
            MapController mapController = new(data, maps.Object, completions)
            { ControllerContext = new() { HttpContext = http } };
            OkObjectResult mapResult = Assert.IsType<OkObjectResult>(await mapController.PushMapsPlayedAsync(
                new() { Map = new() { MapId = song, Score = 0 } }, sessions, accessor));
            JsonElement mapJson = JsonSerializer.SerializeToElement(mapResult.Value, JsonSerializerOptions.Web);
            Assert.Equal(0, mapJson.GetProperty("earnedXP").GetInt32());
            Assert.False(mapJson.GetProperty("isHighScore").GetBoolean());
            Assert.Equal(hasScore ? 12000 : 0, mapJson.GetProperty("mapStats").GetProperty("highScore").GetInt32());
            Assert.Equal(hasScore ? 1 : 0, mapJson.GetProperty("mapStats").GetProperty("playCount").GetInt32());
            noProgressionWrites.VerifyNoOtherCalls();

            PlaylistController playlistController = new(data, maps.Object)
            { ControllerContext = new() { HttpContext = http } };
            OkObjectResult playlistResult = Assert.IsType<OkObjectResult>(await playlistController.PushPlaylistPlayedAsync(
                new() { Playlist = new() { PlaylistId = playlist, TotalScore = 0, MapData = [new() { MapId = song, Score = 0 }] } },
                sessions, accessor));
            JsonElement playlistJson = JsonSerializer.SerializeToElement(playlistResult.Value);
            Assert.False(playlistJson.GetProperty("isHighScore").GetBoolean());
            Assert.Equal(hasScore ? 1 : 0, playlistJson.GetProperty("playlistStats").GetProperty("playCount").GetInt32());
            Assert.Equal(song, playlistJson.GetProperty("playlistStats").GetProperty("highScorePerMap")[0].GetProperty("mapId").GetGuid());

            Mutation mutation = new();
			ExecutionResult<MapStatsResponse> graphMap = await mutation.PushMapsPlayedAsync(new() { Maps = new() { MapId = song, Score = 0 } },
                data, maps.Object, sessions, accessor, completions);
            Assert.True(graphMap.Success);
            Assert.Equal(hasScore ? 1 : 0, graphMap.Response!.MapStats.PlayCount);
			ExecutionResult<PlaylistStatsResponse> graphPlaylist = await mutation.PushPlaylistPlayedAsync(new() { Playlist = new()
            { PlaylistId = playlist, TotalScore = 0, MapData = [new() { MapId = song, Score = 0 }] } },
                data, maps.Object, sessions, accessor);
            Assert.True(graphPlaylist.Success);
            Assert.Equal(hasScore ? 1 : 0, graphPlaylist.Response!.PlaylistStats.PlayCount);

            Assert.True((await scores.MergeMapScoreAsync(new(), 0, 99, player, mapName, "wiiu")).Success);
            db.ChangeTracker.Clear();
            Assert.Equal(before, await SnapshotAsync(db));
        }
        finally
		{
			sessions.RemoveSession(sessionId);
		}
	}

	private static async Task<string> SnapshotAsync(UserDataContext db) => JsonSerializer.Serialize(new
    {
        Maps = await db.MapScores.AsNoTracking().ToListAsync(),
        Playlists = await db.PlaylistScores.AsNoTracking().ToListAsync(),
        Progression = await db.PlayerProgressions.AsNoTracking().ToListAsync()
    });
}
