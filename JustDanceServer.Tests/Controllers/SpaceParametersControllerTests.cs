using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json;

using SpaceParametersController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.Parameters;

namespace JustDanceServer.Tests.Controllers;

public class SpaceParametersControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("live_23.0.0-test")]
    [InlineData("live_24.0.0-test")]
    [InlineData("live_25.0.0-test")]
    [InlineData("live_26.0.1-test")]
    [InlineData("live_27.0.1-test")]
    [InlineData("unknown-build")]
    public void AllBuildsReceiveSharedParameters(string? build)
    {
        var baseline = Read(Controller(null));
        var parameters = Read(Controller(build));
        Assert.Equal(JsonSerializer.Serialize(baseline), JsonSerializer.Serialize(parameters));
        Assert.Equal(true, parameters.Parameters["customClientFeaturesSwitches"].Fields["DecadeMode"]);
        Assert.True(parameters.Parameters["GameplaySettings"].Fields.ContainsKey("DecadeModeSongsList"));
        var social = parameters.Parameters["us-GlobalSpaceConfig"].Fields;
        Assert.Equal(UbisoftServicesConstants.SocialSpaceId, social["FriendsSpaceId"]);
        Assert.Equal(UbisoftServicesConstants.SocialSpaceId, social["BlocklistSpaceId"]);
    }

    [Theory]
    [InlineData("ubi-appbuildid")]
    [InlineData("x-jd-buildid")]
    public void ModernBuildGetsDecadeCaptureSettingsWithoutLosingLocalBootstrap(string header)
    {
        var controller = Controller(null);
        controller.Request.Headers[header] = "live_27.0.1-417080_NX_Final_only-gel_260916";
        var parameters = Read(controller);
        Assert.Equal(true, parameters.Parameters["customClientFeaturesSwitches"].Fields["DecadeMode"]);
        var fields = parameters.Parameters["GameplaySettings"].Fields;
        var songs = Assert.IsType<Dictionary<string, string>>(fields["DecadeModeSongsList"]);
        Assert.Equal(40, songs.Count);
        Assert.Equal("39f33429-8aff-441c-98d4-fb225e04ee3f", songs["IntroSong"]);
        Assert.All(songs.Values, song => Assert.True(Guid.TryParse(song, out _)));
        Assert.Contains("songpack_year5", Assert.IsType<string[]>(fields["DecadeModeRequiredPacks"]));
        Assert.Empty(Assert.IsType<object[]>(fields["DecadeModeQuizBlacklist_Sorting"]));
        Assert.Empty(Assert.IsType<object[]>(fields["DecadeModeQuizBlacklist_TextMcq"]));
        Assert.Equal("0", fields["CameraScoring_MoveScoreBoostPercent"]);
        Assert.Equal(new[] { "songpack_year5", "jdplus" },
            Assert.IsType<System.Collections.Immutable.ImmutableArray<string>>(fields["ClaimDisplayPriority"]));
        Assert.Equal(UbisoftServicesConstants.SocialSpaceId,
            parameters.Parameters["us-GlobalSpaceConfig"].Fields["FriendsSpaceId"]);
        Assert.Equal("https://next.local.test", parameters.Parameters["ServerInfo"].Fields["url"]);
        Assert.Equal("wss://next.local.test/v2/websocket", parameters.Parameters["us-sdkClientUrls"].Fields["websocketNotifications"]);
        controller.Request.Headers.Remove(header);
        var legacyAgain = Read(controller);
        Assert.Equal(JsonSerializer.Serialize(parameters), JsonSerializer.Serialize(legacyAgain));
    }

    private static SpaceParametersController Controller(string? build)
    {
        Mock<IBundleService> bundles = new();
        bundles.Setup(value => value.ClaimDisplayPriority).Returns(["songpack_year5", "jdplus"]);
        var controller = new SpaceParametersController(bundles.Object, Mock.Of<ITagService>(),
            Options.Create(new UrlSettings { HostUrl = "next.local.test" }))
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext() }
        };
        if (build is not null)
            controller.Request.Headers["ubi-appbuildid"] = build;
        return controller;
    }

    private static ParameterList Read(SpaceParametersController controller)
        => Assert.IsType<ParameterList>(Assert.IsType<OkObjectResult>(controller.GetParametersSpaces(JdNextConstants.GameSpaceId)).Value);

    [Fact]
    public void SpaceParametersController_ExposesPcDebugBuildRoute()
    {
        string[] routeTemplates = typeof(SpaceParametersController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray()!;

        Assert.Contains("/v1/spaces/{spaceId:jdnext}/parameters", routeTemplates);
        Assert.Contains("/v2/spaces/{spaceId:jdnext}/parameters", routeTemplates);

        HttpGetAttribute httpGet = Assert.Single(typeof(SpaceParametersController)
            .GetMethod(nameof(SpaceParametersController.GetParametersSpaces))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>());
        Assert.Null(httpGet.Name);
    }
}
