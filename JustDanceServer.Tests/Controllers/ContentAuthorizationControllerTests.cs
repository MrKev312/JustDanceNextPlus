using JustDanceServer.Controllers.prod_next.just_dance.com.songdb.v3.content_authorization.map.mapid.formatVersion;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JustDanceServer.Tests.Controllers;

public sealed class ContentAuthorizationControllerTests
{
    private static readonly Guid MapId = Guid.NewGuid();

    [Theory]
    [InlineData(null)]
    [InlineData("live_23.0.0-test")]
    [InlineData("live_24.0.0-test")]
    [InlineData("live_25.0.0-test")]
    [InlineData("live_26.0.1-test")]
    [InlineData("unknown-build")]
    [InlineData("")]
    [InlineData("live_")]
    [InlineData("live_28")]
    [InlineData("live_.0")]
    [InlineData("live_abc.0")]
    [InlineData("live_+28.0")]
    [InlineData("live_ 28.0")]
    [InlineData("live_999999999999999.0")]
    public async Task LegacyAndUnknownBuildsRequestFourQualities(string? build)
    {
        var (controller, maps, response) = Controller();
        if (build is not null)
            controller.Request.Headers["x-jd-buildid"] = build;
        Assert.Same(response, await Read(controller));
        maps.Verify(service => service.GetContentAuthorization(ClientPlatform.NintendoSwitch, "Moonlight", 4), Times.Once);
    }

    [Theory]
    [InlineData("ubi-appbuildid", "live_27.0.1-417080_NX_Final_only-gel_260916")]
    [InlineData("x-jd-buildid", "live_27.0.1-test")]
    [InlineData("ubi-appbuildid", "live_28.0.1-test")]
    [InlineData("x-jd-buildid", "live_28.0.1-test")]
    [InlineData("ubi-appbuildid", "LIVE_29.0.1-test")]
    [InlineData("x-jd-buildid", "live_30.0.1-test")]
    public async Task ModernBuildsRequestFiveQualities(string header, string build)
    {
        var (controller, maps, response) = Controller();
        controller.Request.Headers[header] = build;
        Assert.Same(response, await Read(controller));
        maps.Verify(service => service.GetContentAuthorization(ClientPlatform.NintendoSwitch, "Moonlight", 5), Times.Once);
    }

    [Theory]
    [InlineData("live_27.0.1-test")]
    [InlineData("live_28.0.1-test")]
    public async Task ModernPcRequestsFourQualities(string build)
    {
        var (controller, maps, response) = Controller();
        controller.Request.Headers.UserAgent = "UbiServices_SDK_Unity_2024.Release.29_PC64_ansi_dynamic";
        controller.Request.Headers["x-jd-buildid"] = build;
        Assert.Same(response, await Read(controller));
        maps.Verify(service => service.GetContentAuthorization(ClientPlatform.PC, "Moonlight", 4), Times.Once);
    }

    [Fact]
    public async Task MissingAuthorizationReturnsNotFound()
    {
        var (controller, maps, _) = Controller();
        maps.Setup(service => service.GetContentAuthorization(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns((ContentAuthorization?)null);
        Assert.IsType<NotFoundResult>(await controller.Get(MapId));
    }

    private static (ContentAuthorizationController Controller, Mock<IMapService> Maps, ContentAuthorization Response) Controller()
    {
        Guid sessionId = Guid.NewGuid();
        Guid playerId = Guid.NewGuid();
        Mock<ISessionManager> sessions = new();
        sessions.Setup(service => service.GetSessionById(sessionId)).Returns(new Session
        {
            SessionId = sessionId,
            PlayerId = playerId,
            UbiAppId = Guid.NewGuid()
        });
        Mock<IUserDataService> users = new();
        users.Setup(service => service.GetProfileByIdAsync(playerId)).ReturnsAsync(new Profile { Id = playerId });
        Mock<IMapService> maps = new();
        maps.SetupGet(service => service.MapToGuid).Returns(new Dictionary<string, Guid> { ["Moonlight"] = MapId });
        ContentAuthorization response = new();
        maps.Setup(service => service.GetContentAuthorization(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(response);
        var controller = new ContentAuthorizationController(NullLogger<ContentAuthorizationController>.Instance,
            maps.Object, sessions.Object, users.Object)
        {
            ControllerContext = new() { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Headers["ubi-sessionid"] = sessionId.ToString();
        controller.Request.Headers.UserAgent = "UbiServices_SDK_Unity_2024.Release.29_SWITCH64";
        return (controller, maps, response);
    }

    private static async Task<ContentAuthorization> Read(ContentAuthorizationController controller)
        => Assert.IsType<ContentAuthorization>(Assert.IsType<OkObjectResult>(await controller.Get(MapId)).Value);
}
