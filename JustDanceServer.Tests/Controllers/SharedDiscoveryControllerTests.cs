using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Tests.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Moq;

using System.Text.Json;
using System.Text.Json.Nodes;

using ActivityController = JustDanceServer.Controllers.prod.just_dance.com.profile.v2.Activity;
using FavoritesController = JustDanceServer.Controllers.prod.just_dance.com.profile.v2.Favorites;
using ProfilesController = JustDanceServer.Controllers.prod.just_dance.com.profile.v2.Profiles;
using WeeklyController = JustDanceServer.Controllers.prod.just_dance.com.leaderboard.v1.maps.mapName.DancerOfTheWeek;
using WeeklyResponse = JustDanceServer.Controllers.prod.just_dance.com.leaderboard.v1.maps.mapName.DancerOfTheWeekResponse;

namespace JustDanceServer.Tests.Controllers;

public sealed class SharedDiscoveryControllerTests
{
	[Fact]
	public async Task FavoriteMutationsRequireASessionAndUseAuthenticatedClientCapabilities()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		using SessionManager sessions = new(new SecurityService());
		DefaultHttpContext http = new();
		FavoritesController controller = new(test.Preferences, sessions) { ControllerContext = new() { HttpContext = http } };
		Assert.IsType<UnauthorizedResult>(await controller.Add("Alpha"));
		(string? ticket, Guid sessionId) = sessions.GenerateSession(test.OtherPlayer, Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d"), "modern");
		http.Request.Headers["ubi-sessionid"] = sessionId.ToString();
		http.Request.Headers["Ubi-AppId"] = Guid.Parse("613fe1a7-86c6-4fa7-bfb3-230e92ae29d3").ToString();
		Assert.IsType<NotFoundResult>(await controller.Add("Alpha"));
		(string ticket, Guid sessionId) legacy = sessions.GenerateSession(test.Player, Guid.Parse("613fe1a7-86c6-4fa7-bfb3-230e92ae29d3"), "legacy");
		http.Request.Headers["ubi-sessionid"] = legacy.sessionId.ToString();
		http.Request.Headers["Ubi-AppId"] = Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d").ToString();
		Assert.IsType<OkResult>(await controller.Add("Alpha"));
		Assert.Equal(["Alpha"], await test.Preferences.GetFavoritesAsync(test.Player));
		Assert.Empty(await test.Preferences.GetFavoritesAsync(test.OtherPlayer));
		Assert.IsType<OkResult>(await controller.Remove("Alpha"));
		Assert.Empty(await test.Preferences.GetFavoritesAsync(test.Player));
	}

	[Fact]
	public async Task Jd2017SnapshotDoesNotUndoFavoriteMutationsOrManufactureDatedScores()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		await test.Preferences.SetFavoriteAsync(test.Player, "Alpha", true);
		using SessionManager sessions = new(new SecurityService());
		(string? ticket, Guid sessionId) = sessions.GenerateSession(test.Player, Guid.Parse("613fe1a7-86c6-4fa7-bfb3-230e92ae29d3"), "legacy");
		DefaultHttpContext http = new();
		http.Request.Headers["ubi-sessionid"] = sessionId.ToString();
		Mock<IClassicProfileService> profiles = new();
		profiles.Setup(value => value.SaveProfileAsync(test.Player, It.IsAny<JsonElement>(), It.IsAny<string>())).ReturnsAsync(true);
		profiles.Setup(value => value.GetProfilesAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(
			[new JsonObject { ["profileId"] = test.Player.ToString(), ["favorites"] = new JsonArray("Beta") }]);
		ProfilesController controller = new(profiles.Object, sessions, test.Preferences, test.Activity)
		{ ControllerContext = new() { HttpContext = http } };
		Assert.IsType<OkResult>(await controller.SaveProfile(JsonSerializer.SerializeToElement(new
		{
			profileId = test.OtherPlayer, favorites = Array.Empty<string>(), history = new { Alpha = 42 }
		})));
		Assert.Equal(["Alpha"], await test.Preferences.GetFavoritesAsync(test.Player));
		PlayerMapOrdinal ordinal = await test.Db.PlayerMapOrdinals.SingleAsync();
		Assert.Equal(test.Player, ordinal.ProfileId);
		Assert.Equal($"jd2017:{sessionId:N}", ordinal.SourceScope);
		Assert.Equal(42, ordinal.Ordinal);
		Assert.Empty(await test.Db.PlayerMapPlays.ToListAsync());
		OkObjectResult response = Assert.IsType<OkObjectResult>(await controller.GetProfiles(test.Player.ToString()));
		JsonObject profile = Assert.Single(Assert.IsType<IReadOnlyList<JsonObject>>(response.Value, exactMatch: false));
		Assert.Equal("Alpha", profile["favorites"]![0]!.ToString());
	}

	[Theory]
	[InlineData(2016, false, false)]
	[InlineData(2017, true, false)]
	[InlineData(2018, false, false)]
	[InlineData(2019, false, true)]
	[InlineData(2020, false, true)]
	[InlineData(2021, false, true)]
	[InlineData(2022, false, true)]
	public void CapabilitiesStayEditionSpecific(int year, bool favorites, bool history)
	{
		PlayerRequestContext context = new(Guid.NewGuid(), ProgressionFamily.JDUnlimited, year, "switch", "en-US");
		Assert.Equal(favorites, context.SupportsFavoriteMutations);
		Assert.Equal(history, context.SupportsMapHistory);
		Assert.False((context with { Family = ProgressionFamily.JDPlus }).SupportsFavoriteMutations);
		Assert.False((context with { Family = ProgressionFamily.JDPlus }).SupportsMapHistory);
	}

	[Fact]
	public async Task ModernActivityAuthenticatesTheUploaderAndAcceptsTheCapturedArrayShape()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		using SessionManager sessions = new(new SecurityService());
		DefaultHttpContext http = new();
		ActivityController controller = new(test.Activity, sessions) { ControllerContext = new() { HttpContext = http } };
		JduMapEndedEntry[] history = JsonSerializer.Deserialize<JduMapEndedEntry[]>($$"""
			[{"mapName":"Alpha","score":12000,"timestamp":{{test.Clock.Now.ToUnixTimeSeconds()}},"gameMode":"classic",
			"nbPlayers":2,"position":1,"coop":false,"wdf":false,"kids":false,"playlist":false,"favPlaylist":false,"jduEnabled":true}]
			""", JsonSerializerOptions.Web)!;
		Assert.IsType<UnauthorizedResult>(await controller.MapEnded(history));
		(string? ticket, Guid sessionId) = sessions.GenerateSession(test.Player, Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d"), "modern");
		http.Request.Headers["ubi-sessionid"] = sessionId.ToString();
		Assert.IsType<OkResult>(await controller.MapEnded(history));
		PlayerMapPlay play = await test.Db.PlayerMapPlays.SingleAsync();
		Assert.Equal(test.Player, play.ProfileId);
		Assert.Equal(ScoreAttribution.SessionBest, play.Attribution);
		Assert.False(play.WeeklyEligible);
	}

	[Fact]
	public async Task WeeklyAdapterReturnsTheEligibleScoreAndNoAllTimeFallback()
	{
		await using DiscoveryTestContext test = new();
		await test.InitializeAsync();
		await test.Scores.RecordMapPlayAsync(new(), 13000, test.Player, "Alpha", "switch");
		WeeklyController controller = new(new LeaderboardService(test.Db), test.Activity);
		WeeklyResponse empty = Assert.IsType<WeeklyResponse>(Assert.IsType<OkObjectResult>(await controller.GetDancerOfTheWeek("Alpha")).Value);
		Assert.Equal(Guid.Empty, empty.ProfileId);
		await test.Activity.RecordChallengeAsync(Guid.NewGuid(), test.Player, "Alpha", 11000, "switch");
		WeeklyResponse winner = Assert.IsType<WeeklyResponse>(Assert.IsType<OkObjectResult>(await controller.GetDancerOfTheWeek("Alpha")).Value);
		Assert.Equal(test.Player, winner.ProfileId);
		Assert.Equal(11000, winner.Score);
		Assert.Equal(1, winner.Rank);
		Assert.Equal("Player", winner.Name);
	}
}
