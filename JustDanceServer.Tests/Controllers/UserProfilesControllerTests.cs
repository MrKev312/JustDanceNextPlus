using Microsoft.AspNetCore.Mvc;

using UserController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.users.PlayerId;

namespace JustDanceServer.Tests.Controllers;

public class UserProfilesControllerTests
{
    [Fact]
    public void UserController_ExposesPcDebugLinkedProfilesRoute()
    {
        HttpGetAttribute httpGet = Assert.Single(typeof(UserController)
            .GetMethod(nameof(UserController.GetLinkedProfiles))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>());

        Assert.Equal("profiles", httpGet.Template);
    }
}
