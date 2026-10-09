using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json;

using ModernJduProfilesController = JustDanceServer.Controllers.prod.just_dance.com.profile.v2.Profiles;
using ModernJduSongDbController = JustDanceServer.Controllers.prod.just_dance.com.songdb.v2.Songs;
using ModernJduSubscriptionController = JustDanceServer.Controllers.prod.just_dance.com.subscription.v1.Refresh;
using ModernJduWdfController = JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.AssignRoom;
using JustDanceServer.Controllers.prod.just_dance.com.songdb.v2;
using JustDanceServer.Controllers.prod.just_dance.com.subscription.v1;
using JustDanceServer.Controllers.prod.just_dance.com.wdf.v1;

namespace JustDanceServer.Tests.Controllers;

public sealed class ModernJduConnectionTests
{
    private static readonly IOptions<UrlSettings> LocalUrls = Options.Create(new UrlSettings
    {
        HostUrl = "next.local.test",
        JduHostUrl = "jdu.local.test",
        CDNUrl = "next.local.test"
    });

    [Fact]
    public void SongDbBootstrap_UsesModernJduHost()
    {
        ModernJduSongDbController controller = new(LocalUrls);

		OkObjectResult result = Assert.IsType<OkObjectResult>(controller.GetSongs("jd2022-nx-all"));
		SongDbResponse response = Assert.IsType<JustDanceServer.Controllers.prod.just_dance.com.songdb.v2.SongDbResponse>(result.Value);

		Assert.Equal("https://jdu.local.test/songdb/v2/database?assetPlatform=NX", response.SongdbUrl);
        Assert.Equal("https://jdu.local.test/songdb/v2/localisation", response.LocalisationUrl);
    }

    [Fact]
    public void SubscriptionRefresh_AdvertisesActiveModernJduSubscription()
    {
        ModernJduSubscriptionController controller = new();
        JsonElement request = JsonSerializer.SerializeToElement(new
        {
            deviceId = Guid.Parse("b3febe1f-e274-5413-968d-18812573103b")
        });

        OkObjectResult result = Assert.IsType<OkObjectResult>(controller.RefreshSubscription(request));
		SubscriptionRefreshResponse response = Assert.IsType<JustDanceServer.Controllers.prod.just_dance.com.subscription.v1.SubscriptionRefreshResponse>(result.Value);

        Assert.True(response.Validity);
        Assert.True(response.TimeLeft > 0);
        Assert.Equal(JduConstants.SubscriptionPlatformId, response.PlatformId);
        Assert.True(response.TrialActivated);
        Assert.True(response.TrialIsActive);
        Assert.True(response.AutoRefresh);

        JsonElement wireResponse = JsonSerializer.SerializeToElement(
            response,
            JsonSerializerOptions.Web);
        Assert.True(wireResponse.GetProperty("validity").GetBoolean());
        Assert.Equal(
            JduConstants.SubscriptionPlatformId,
            wireResponse.GetProperty("platformId").GetGuid());
    }

    [Fact]
    public async Task ProfilesEndpoint_ReturnsNoOnlineDancerProfiles()
    {
        Mock<IClassicProfileService> profiles = new();
        profiles.Setup(service => service.GetProfilesAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync([]);
        ModernJduProfilesController controller = new(profiles.Object, Mock.Of<ISessionManager>(),
			Mock.Of<IPlayerPreferenceService>(), Mock.Of<IPlayerActivityService>())
		{
			ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() }
		};

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            await controller.GetProfiles(null));
        IReadOnlyList<System.Text.Json.Nodes.JsonObject> response =
            Assert.IsType<IReadOnlyList<System.Text.Json.Nodes.JsonObject>>(result.Value, exactMatch: false);

        Assert.Empty(response);
    }

    [Fact]
    public void WdfAssignment_UsesTheRoomSelectedForTheClientYear()
    {
		Mock<IWdfService> wdf = new();
		wdf.Setup(service => service.GetAssignedRoom(2022)).Returns(WdfRoomId.HappyHour);
		ModernJduWdfController controller = new(
			wdf.Object,
			Mock.Of<ISessionManager>())
		{
			ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
		};
		controller.Request.Headers["X-SkuId"] = "jd2022-nx-all";

        OkObjectResult result = Assert.IsType<OkObjectResult>(controller.Assign());
		RoomInfoResponse response = Assert.IsType<JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.RoomInfoResponse>(result.Value);

        Assert.Equal(WdfRoomId.HappyHour.Name, response.Room);
    }
}
