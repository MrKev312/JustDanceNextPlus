using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using System.Text.Json;

using FriendsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.profiles.other.Friends;

namespace JustDanceServer.Tests.Controllers;

public sealed class FriendsControllerTests
{
    [Fact]
    public async Task LegacyAndModernGetsProjectTheSameSharedRelationship()
    {
        Guid profileId = Guid.NewGuid();
        Guid friendProfileId = Guid.NewGuid();
        Mock<IFriendService> friendService = new();
        friendService.Setup(service => service.GetRelationshipsAsync(profileId))
            .ReturnsAsync([new FriendRelationship(friendProfileId, "Friends")]);

        FriendsController controller = CreateController(profileId, friendService.Object);
        OkObjectResult legacy = Assert.IsType<OkObjectResult>(await controller.GetLegacyFriends());
        OkObjectResult modern = Assert.IsType<OkObjectResult>(await controller.GetFriends(profileId));

        JsonElement legacyJson = JsonSerializer.SerializeToElement(
            legacy.Value,
            JsonSerializerOptions.Web);
        JsonElement modernJson = JsonSerializer.SerializeToElement(
            modern.Value,
            JsonSerializerOptions.Web);
        Assert.Equal(legacyJson.GetRawText(), modernJson.GetRawText());
        JsonElement friend = Assert.Single(legacyJson.GetProperty("friends").EnumerateArray());
        Assert.Equal(friendProfileId, friend.GetProperty("pid").GetGuid());
        Assert.Equal("Friends", friend.GetProperty("state").GetString());
    }

    [Fact]
    public async Task RouteProfileCannotImpersonateAnotherSession()
    {
        Guid profileId = Guid.NewGuid();
        FriendsController controller = CreateController(profileId, Mock.Of<IFriendService>());

        StatusCodeResult result = Assert.IsType<StatusCodeResult>(await controller.GetFriends(Guid.NewGuid()));
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        using ServiceProvider provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        controller.HttpContext.RequestServices = provider;
        await result.ExecuteResultAsync(new ActionContext(controller.HttpContext, new(), new()));
        Assert.Equal(StatusCodes.Status403Forbidden, controller.Response.StatusCode);
    }

    private static FriendsController CreateController(
        Guid profileId,
        IFriendService friendService)
    {
        Guid sessionId = Guid.NewGuid();
        Session? session = new()
        {
            SessionId = sessionId,
            PlayerId = profileId,
            UbiAppId = Guid.NewGuid()
        };
        Mock<ISessionManager> sessionManager = new();
        sessionManager
            .Setup(manager => manager.TryGetSessionById(sessionId, out session))
            .Returns(true);

        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers["Ubi-SessionId"] = sessionId.ToString();
        return new FriendsController(friendService, sessionManager.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
