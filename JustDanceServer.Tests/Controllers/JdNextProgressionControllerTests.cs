using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using System.Text.Json;
using System.Text.Json.Nodes;

using ProgressionController = JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.ProgressionInfo;
using PlaylistProfilesController = JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile.leaderboard.playlist.Profiles;

namespace JustDanceServer.Tests.Controllers;

public sealed class JdNextProgressionControllerTests
{
    private static readonly Guid SongId = Guid.Parse("12345678-1234-4321-8765-123456789012");
    private static readonly Guid PlaylistId = Guid.Parse("87654321-4321-1234-8765-210987654321");
    private const string MapName = "TestSong";
    private const string UnavailableMap = "NotInJdNextCatalog";

    [Fact]
    public async Task ProgressionInfo_UsesSongIdsWithoutChangingTheStoredScoresOrOtherFields()
    {
        Profile profile = CreateProfile();
        Mock<IUserDataService> users = new();
        users.Setup(service => service.GetProfileByIdAsync(profile.Id)).ReturnsAsync(profile);
        Guid sessionId = Guid.NewGuid();
        Mock<ISessionManager> sessions = CreateSession(sessionId, profile.Id);
        ProgressionController controller = new(sessions.Object, users.Object, CreateMaps().Object)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["ubi-sessionid"] = sessionId.ToString();

        JsonNode? actual = Serialize(Assert.IsType<OkObjectResult>(await controller.GetProgressionInfo()).Value);
        JsonNode expected = Serialize(profile)!;
        expected["mapStats"] = new JsonObject { [SongId.ToString()] = Serialize(profile.MapStats[MapName]) };
        expected["playlistStats"]![PlaylistId.ToString()]!["highScorePerMap"] = new JsonObject
        {
            [SongId.ToString()] = profile.PlaylistStats[PlaylistId].HighScorePerMap[MapName]
        };

        Assert.True(JsonNode.DeepEquals(expected, actual), actual?.ToJsonString());
        AssertStoredKeysUnchanged(profile);
    }

    [Fact]
    public async Task PlaylistLeaderboard_UsesSongIdsAndKeepsMissingPlaylistStatsNull()
    {
        Profile profile = CreateProfile();
        Profile unplayed = new() { Id = Guid.NewGuid() };
        Mock<IUserDataService> users = new();
        users.Setup(service => service.GetProfileByIdAsync(profile.Id)).ReturnsAsync(profile);
        users.Setup(service => service.GetProfileByIdAsync(unplayed.Id)).ReturnsAsync(unplayed);
        PlaylistProfilesController controller = new(users.Object, CreateMaps().Object);

        JsonNode? actual = Serialize(Assert.IsType<OkObjectResult>(await controller.GetProfiles(new()
        {
            PlaylistId = PlaylistId,
            UsUserIds = [profile.Id, unplayed.Id, Guid.NewGuid()]
        })).Value);
        JsonNode expected = Serialize(new
        {
            Profiles = new[]
            {
                new { profile.Id, profile.Dancercard, PlaylistStat = (PlaylistStats?)profile.PlaylistStats[PlaylistId], profile.Ownership },
                new { unplayed.Id, unplayed.Dancercard, PlaylistStat = (PlaylistStats?)null, unplayed.Ownership }
            }
        })!;
        expected["profiles"]![0]!["playlistStat"]!["highScorePerMap"] = new JsonObject
        {
            [SongId.ToString()] = profile.PlaylistStats[PlaylistId].HighScorePerMap[MapName]
        };

        Assert.True(JsonNode.DeepEquals(expected, actual), actual?.ToJsonString());
        AssertStoredKeysUnchanged(profile);
    }

    [Fact]
    public async Task ProgressionInfo_MissingProfileStillReturnsNull()
    {
        Guid sessionId = Guid.NewGuid();
        ProgressionController controller = new(
            CreateSession(sessionId, Guid.NewGuid()).Object,
            Mock.Of<IUserDataService>(), CreateMaps().Object)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["ubi-sessionid"] = sessionId.ToString();

        Assert.Null(Assert.IsType<OkObjectResult>(await controller.GetProgressionInfo()).Value);
    }

    private static Mock<ISessionManager> CreateSession(Guid sessionId, Guid playerId)
    {
        Mock<ISessionManager> sessions = new();
        sessions.Setup(service => service.GetSessionById(sessionId)).Returns(new Session
        {
            SessionId = sessionId,
            PlayerId = playerId,
            UbiAppId = Guid.NewGuid()
        });
        return sessions;
    }

    private static Mock<IMapService> CreateMaps()
    {
        Mock<IMapService> maps = new();
        maps.Setup(service => service.GetSongId(MapName)).Returns(SongId);
        maps.Setup(service => service.GetSongId(UnavailableMap)).Returns((Guid?)null);
        return maps;
    }

    private static Profile CreateProfile() => new()
    {
        Id = Guid.NewGuid(),
        Ticket = "must-not-be-serialized",
        CurrentXP = 3456,
        CurrentLevel = 7,
        PrestigeGrade = 2,
        Ownership = new() { Claims = ["test-claim"] },
        MapStats = new(StringComparer.OrdinalIgnoreCase)
        {
            [MapName] = new() { MapName = MapName, HighScore = 12000, PlayCount = 3, Platform = "switch" },
            [UnavailableMap] = new() { MapName = UnavailableMap, HighScore = 11000, PlayCount = 2 }
        },
        PlaylistStats = new()
        {
            [PlaylistId] = new()
            {
                PlaylistId = PlaylistId,
                HighScore = 23000,
                PlayCount = 4,
                Platform = "switch",
                HighScorePerMap = new(StringComparer.OrdinalIgnoreCase) { [MapName] = 12000, [UnavailableMap] = 11000 }
            }
        }
    };

    private static void AssertStoredKeysUnchanged(Profile profile)
    {
        Assert.Equal(new[] { MapName, UnavailableMap }, profile.MapStats.Keys);
        Assert.Equal(new[] { MapName, UnavailableMap }, profile.PlaylistStats[PlaylistId].HighScorePerMap.Keys);
        Assert.Equal(11000, profile.MapStats[UnavailableMap].HighScore);
        Assert.Equal(11000, profile.PlaylistStats[PlaylistId].HighScorePerMap[UnavailableMap]);
    }

    private static JsonNode? Serialize(object? value) => JsonSerializer.SerializeToNode(value, JsonSerializerOptions.Web);
}
