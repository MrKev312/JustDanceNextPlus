using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        Guid appId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        Profile profile = new()
        {
            Id = Guid.NewGuid(),
            Ticket = expectedTicket,
            Dancercard = new DancerCard { Name = "PC Player" }
        };

        Mock<IUserDataService> userDataService = new();
        userDataService
            .Setup(service => service.GetProfileByTicketAsync(expectedTicket))
            .Returns(new ValueTask<Profile?>(profile));

        Mock<ITimingService> timingService = new();
        timingService.Setup(service => service.TimeString(It.IsAny<DateTime>())).Returns("2026-05-20T20:00:00Z");
        timingService.Setup(service => service.ServerTime()).Returns("2026-05-20T20:00:00Z");

        Mock<ISessionManager> sessionManager = new();
        sessionManager.SetupGet(manager => manager.SessionExpirationTimeSpan).Returns(TimeSpan.FromHours(3));
        sessionManager
            .Setup(manager => manager.GenerateOrRefreshSession(profile.Id, appId, expectedTicket))
            .Returns(("session-ticket", sessionId));

        SessionsController controller = CreateController(
            userDataService.Object,
            timingService.Object,
            sessionManager.Object,
            appId,
            authorization);

        using JsonDocument body = JsonDocument.Parse("{}");
        IActionResult result = await controller.PostSessions(body.RootElement);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SessionResponse response = Assert.IsType<SessionResponse>(ok.Value);
        Assert.Equal(profile.Id, response.ProfileId);
        Assert.Equal("session-ticket", response.Ticket);
        userDataService.Verify(service => service.GetProfileByTicketAsync(expectedTicket), Times.Once);
        sessionManager.Verify(manager => manager.GenerateOrRefreshSession(profile.Id, appId, expectedTicket), Times.Once);
    }

    [Fact]
    public async Task PostSessions_RejectsUnsupportedAuthorizationScheme()
    {
        SessionsController controller = CreateController(
            Mock.Of<IUserDataService>(),
            Mock.Of<ITimingService>(),
            Mock.Of<ISessionManager>(),
            Guid.NewGuid(),
            "Bearer unsupported");

        using JsonDocument body = JsonDocument.Parse("{}");
        IActionResult result = await controller.PostSessions(body.RootElement);

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid 'Authorization' header in request", badRequest.Value);
    }

    private static SessionsController CreateController(
        IUserDataService userDataService,
        ITimingService timingService,
        ISessionManager sessionManager,
        Guid appId,
        string authorization)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["Ubi-AppId"] = appId.ToString();
        httpContext.Request.Headers["Authorization"] = authorization;

        return new SessionsController(userDataService, timingService, sessionManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }
}
