using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json;

using SessionsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.profiles.Sessions;
using SessionResponse = JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.profiles.SessionResponse;

namespace JustDanceServer.Tests.Controllers;

public class SessionsControllerTests
{
    [Theory]
    [InlineData("Basic dGVzdEBnbWFpbC5jb206dGVzdA==", "test@gmail.com")]
    [InlineData("uplaypc_v1 t=pc-ticket", "pc-ticket")]
    public async Task PostSessions_AcceptsPcAuthorizationSchemes(string authorization, string expectedTicket)
    {
        Guid appId = JdNextConstants.PcApplicationId;
        Guid sessionId = Guid.NewGuid();
        Guid profileId = Guid.NewGuid();
        const string sessionKey = "resolved-pc-account";
        Mock<IPlayerIdentityService> identityService = new();
        identityService
            .Setup(service => service.ResolveOrCreateAsync(
                It.Is<SessionLoginIdentity>(login => login.LegacyTicket == expectedTicket)))
            .ReturnsAsync(new ResolvedPlayerIdentity(
                profileId,
                "uplay",
                "pc-player",
                "PC Player",
                sessionKey));

        Mock<ITimingService> timingService = new();
        timingService.Setup(service => service.TimeString(It.IsAny<DateTime>())).Returns("2026-05-20T20:00:00Z");
        timingService.Setup(service => service.ServerTime()).Returns("2026-05-20T20:00:00Z");

        Mock<ISessionManager> sessionManager = new();
        sessionManager.SetupGet(manager => manager.SessionExpirationTimeSpan).Returns(TimeSpan.FromHours(3));
        string applicationSessionKey = SessionIdentityKey.ForApplication(sessionKey, appId);
        sessionManager
            .Setup(manager => manager.GenerateOrRefreshSession(profileId, appId, applicationSessionKey))
            .Returns(("session-ticket", sessionId));

        SessionsController controller = CreateController(
            identityService.Object,
            timingService.Object,
            sessionManager.Object,
            appId,
            authorization);

        using JsonDocument body = JsonDocument.Parse("{}");
        IActionResult result = await controller.PostSessions(body.RootElement);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SessionResponse response = Assert.IsType<SessionResponse>(ok.Value);
        Assert.Equal(profileId, response.ProfileId);
        Assert.Equal("session-ticket", response.Ticket);
        identityService.Verify(service => service.ResolveOrCreateAsync(
            It.Is<SessionLoginIdentity>(login => login.LegacyTicket == expectedTicket)), Times.Once);
        sessionManager.Verify(manager => manager.GenerateOrRefreshSession(profileId, appId, applicationSessionKey), Times.Once);
    }

    [Fact]
    public async Task PostSessions_RejectsUnsupportedAuthorizationScheme()
    {
        SessionsController controller = CreateController(
            Mock.Of<IPlayerIdentityService>(),
            Mock.Of<ITimingService>(),
            Mock.Of<ISessionManager>(),
            JdNextConstants.PcApplicationId,
            "Bearer unsupported");

        using JsonDocument body = JsonDocument.Parse("{}");
        IActionResult result = await controller.PostSessions(body.RootElement);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid 'Authorization' header in request", badRequest.Value);
    }

    [Fact]
    public void DeleteSessionRemovesTheAuthenticatedSession()
    {
        Guid sessionId = Guid.NewGuid();
        Session? session = new()
        {
            SessionId = sessionId,
            PlayerId = Guid.NewGuid(),
            UbiAppId = JdNextConstants.PcApplicationId
        };
        Mock<ISessionManager> sessionManager = new();
        sessionManager.Setup(value => value.TryGetSessionById(sessionId, out session)).Returns(true);
        sessionManager.Setup(value => value.TouchSession(sessionId)).Returns(true);
        sessionManager.Setup(value => value.RemoveSession(sessionId)).Returns(true);
        SessionsController controller = CreateController(
            Mock.Of<IPlayerIdentityService>(),
            Mock.Of<ITimingService>(),
            sessionManager.Object,
            JdNextConstants.PcApplicationId,
            string.Empty);
        controller.Request.Headers["ubi-sessionid"] = sessionId.ToString();

        Assert.IsType<OkResult>(controller.DeleteSession());
        sessionManager.Verify(value => value.RemoveSession(sessionId), Times.Once);
    }

    [Fact]
    public async Task PostSessions_ReturnsJdu2022SwitchIdentity()
    {
        const string ticket = "jdu-ticket";
        Guid profileId = Guid.NewGuid();
        const string sessionKey = "resolved-jdu-account";
        Mock<IPlayerIdentityService> identityService = new();
        identityService
            .Setup(service => service.ResolveOrCreateAsync(It.IsAny<SessionLoginIdentity>()))
            .ReturnsAsync(new ResolvedPlayerIdentity(
                profileId,
                "switch",
                "nintendo-player",
                "JDU Player",
                sessionKey));

        Mock<ITimingService> timingService = new();
        timingService.Setup(service => service.TimeString(It.IsAny<DateTime>())).Returns("2026-08-01T12:00:00Z");
        timingService.Setup(service => service.ServerTime()).Returns("2026-08-01T12:00:00Z");

        Mock<ISessionManager> sessionManager = new();
        sessionManager.SetupGet(manager => manager.SessionExpirationTimeSpan).Returns(TimeSpan.FromHours(3));
        string applicationSessionKey = SessionIdentityKey.ForApplication(
            sessionKey,
            Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d"));
        sessionManager
            .Setup(manager => manager.GenerateOrRefreshSession(profileId, Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d"), applicationSessionKey))
            .Returns(("session-ticket", Guid.NewGuid()));

        SessionsController controller = CreateController(
            identityService.Object,
            timingService.Object,
            sessionManager.Object,
            Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d"),
            $"uplaypc_v1 t={ticket}");

        using JsonDocument body = JsonDocument.Parse("{}");
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.PostSessions(body.RootElement));
        SessionResponse response = Assert.IsType<SessionResponse>(result.Value);

        Assert.Equal("switch", response.PlatformType);
        Assert.Equal(Guid.Parse("c0e6c86a-09ba-4b31-afa6-c6dd0759ff0d"), response.SpaceId);
    }

    [Theory]
    [MemberData(nameof(WiiUClients))]
    public async Task PostSessions_AcceptsCapturedWiiUCredentials(string applicationIdText, string expectedSpaceIdText)
    {
        Guid applicationId = Guid.Parse(applicationIdText);
        Guid expectedSpaceId = Guid.Parse(expectedSpaceIdText);
        const string consoleToken = "c768ad1e29b04f3280a64d7c441f07637d9f6d957cb14ac69df92de409a0f02e";
        Guid profileId = Guid.NewGuid();
        const string sessionKey = "nintendo-network:1327407795";
        SessionLoginIdentity? capturedIdentity = null;
        Mock<IPlayerIdentityService> identityService = new();
        identityService
            .Setup(service => service.ResolveOrCreateAsync(It.IsAny<SessionLoginIdentity>()))
            .Callback<SessionLoginIdentity>(login => capturedIdentity = login)
            .ReturnsAsync(new ResolvedPlayerIdentity(
                profileId,
                "wiiu",
                "1327407795",
                "MrBob312",
                sessionKey));

        Mock<ITimingService> timingService = new();
        timingService.Setup(service => service.TimeString(It.IsAny<DateTime>())).Returns("2026-08-04T22:00:00Z");
        timingService.Setup(service => service.ServerTime()).Returns("2026-08-04T22:00:00Z");

        Guid sessionId = Guid.NewGuid();
        Mock<ISessionManager> sessionManager = new();
        sessionManager.SetupGet(manager => manager.SessionExpirationTimeSpan).Returns(TimeSpan.FromHours(3));
        string applicationSessionKey = SessionIdentityKey.ForApplication(sessionKey, applicationId);
        sessionManager
            .Setup(manager => manager.GenerateOrRefreshSession(profileId, applicationId, applicationSessionKey))
            .Returns(("session-ticket", sessionId));

        SessionsController controller = CreateController(
            identityService.Object,
            timingService.Object,
            sessionManager.Object,
            applicationId,
            $"wiiu t={consoleToken}");

        using JsonDocument body = JsonDocument.Parse("""
            {"nameOnPlatform":"MrBob312","idOnPlatform":1327407795}
            """);
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.PostSessions(body.RootElement));
        SessionResponse response = Assert.IsType<SessionResponse>(result.Value);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("nintendo-network", capturedIdentity.Provider);
        Assert.Equal("1327407795", capturedIdentity.Subject);
        Assert.Equal("1327407795", capturedIdentity.IdOnPlatform);
        Assert.Equal(consoleToken, capturedIdentity.LegacyTicket);
        Assert.Equal("wiiu", response.PlatformType);
        Assert.Equal(expectedSpaceId, response.SpaceId);
        Assert.True(response.HasAcceptedLegalOptins);
        Assert.Empty(response.AccountIssues);
        Assert.Equal(64, Convert.FromBase64String(response.SessionKey).Length);
        Assert.Equal("session-ticket", response.Ticket);

        JsonElement wireResponse = JsonSerializer.SerializeToElement(
            response,
            JsonSerializerOptions.Web);
        Assert.True(wireResponse.GetProperty("hasAcceptedLegalOptins").GetBoolean());
        Assert.Equal(JsonValueKind.Array, wireResponse.GetProperty("accountIssues").ValueKind);
        sessionManager.Verify(
            manager => manager.GenerateOrRefreshSession(profileId, applicationId, applicationSessionKey),
            Times.Once);
    }

    [Fact]
    public void SessionsController_ServesV2AndV3Routes()
    {
        string?[] routes = [.. typeof(SessionsController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)];

        Assert.Contains("v2/profiles/sessions", routes);
        Assert.Contains("v3/profiles/sessions", routes);
    }

    [Theory]
    [MemberData(nameof(JduClients))]
    public async Task PostSessions_ReturnsJduIdentity(
        string applicationIdText,
        string expectedSpaceIdText,
        string expectedPlatformType)
    {
        Guid applicationId = Guid.Parse(applicationIdText);
        Guid expectedSpaceId = Guid.Parse(expectedSpaceIdText);
        const string ticket = "classic-jdu-ticket";
        Guid profileId = Guid.NewGuid();
        const string sessionKey = "resolved-classic-jdu-account";
        Mock<IPlayerIdentityService> identityService = new();
        identityService
            .Setup(service => service.ResolveOrCreateAsync(It.IsAny<SessionLoginIdentity>()))
            .ReturnsAsync(new ResolvedPlayerIdentity(
                profileId,
                expectedPlatformType,
                "platform-player",
                "Classic JDU Player",
                sessionKey));

        Mock<ITimingService> timingService = new();
        timingService.Setup(service => service.TimeString(It.IsAny<DateTime>())).Returns("2026-08-03T12:00:00Z");
        timingService.Setup(service => service.ServerTime()).Returns("2026-08-03T12:00:00Z");

        Mock<ISessionManager> sessionManager = new();
        sessionManager.SetupGet(manager => manager.SessionExpirationTimeSpan).Returns(TimeSpan.FromHours(3));
        string applicationSessionKey = SessionIdentityKey.ForApplication(sessionKey, applicationId);
        sessionManager
            .Setup(manager => manager.GenerateOrRefreshSession(profileId, applicationId, applicationSessionKey))
            .Returns(("session-ticket", Guid.NewGuid()));

        SessionsController controller = CreateController(
            identityService.Object,
            timingService.Object,
            sessionManager.Object,
            applicationId,
            $"uplaypc_v1 t={ticket}");

        using JsonDocument body = JsonDocument.Parse("{}");
        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.PostSessions(body.RootElement));
        SessionResponse response = Assert.IsType<SessionResponse>(result.Value);

        Assert.Equal(expectedPlatformType, response.PlatformType);
        Assert.Equal(expectedSpaceId, response.SpaceId);
    }

    public static TheoryData<string, string, string> JduClients
    {
        get
        {
            TheoryData<string, string, string> data = new();
            foreach (IUbisoftClientProfile client in JduApplicationClientProfiles.All)
                data.Add(client.ApplicationId.ToString(), client.SpaceId.ToString(), client.PlatformType);
            return data;
        }
    }

    public static TheoryData<string, string> WiiUClients => new()
    {
        { Guid.Parse("3696a38b-1edd-4e7b-bbb7-855f15a25408").ToString(), Guid.Parse("17599f67-8a3b-407d-ac5e-7dfdd66d5194").ToString() },
        { Guid.Parse("81cf5bb0-e037-4374-b38d-106bd21307fa").ToString(), Guid.Parse("96f82187-4d45-4544-9718-0cde2f8d5ccb").ToString() },
        { Guid.Parse("f8c1989f-ea92-4567-b43d-436de74b2c5d").ToString(), Guid.Parse("807358a9-c9e5-4d5b-be84-e5e7b98d0421").ToString() },
        { Guid.Parse("3b4009a1-66cb-410d-9799-3041835d8e1b").ToString(), Guid.Parse("ef680ba3-e1ae-4fb5-b434-ab448f1c3210").ToString() }
    };

    private static SessionsController CreateController(
        IPlayerIdentityService identityService,
        ITimingService timingService,
        ISessionManager sessionManager,
        Guid appId,
        string authorization)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["Ubi-AppId"] = appId.ToString();
        httpContext.Request.Headers.Authorization = authorization;

        return new SessionsController(
            identityService,
            new SessionLoginIdentityParser(new PlatformTicketVerifierRegistry(
                [new DevelopmentPlatformTicketVerifier(Options.Create(
                    new PlatformAuthenticationSettings
                    {
                        AllowUnverifiedPlatformTickets = true
                    }))]),
                Mock.Of<IBasicCredentialVerifier>(verifier => verifier.Verify("test@gmail.com", "test") == true)),
            timingService,
            sessionManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }
}
