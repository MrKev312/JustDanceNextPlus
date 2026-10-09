using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using System.Text.Json;

using Jdu2019ConfigurationController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.applications.Configuration;
using JduEntitiesController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces.JduEntities;
using PopulationsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.Populations;
using UbisoftProfilesController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v3.Profiles;

namespace JustDanceServer.Tests.Controllers;

public sealed class Jdu2019ApplicationConfigurationTests
{
    private static readonly Microsoft.Extensions.Options.IOptions<UrlSettings> LocalUrls =
        Microsoft.Extensions.Options.Options.Create(new UrlSettings
        {
            HostUrl = "next.local.test",
            JduHostUrl = "jdu.local.test",
            CDNUrl = "next.local.test"
        });

    [Fact]
    public void Jdu2019SwitchRoute_ReturnsCapturedApplicationAndSpaceIdentity()
    {
		Jdu2019ConfigurationController controller = new(new ModernJduApplicationConfigurationService())
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			}
		};
		controller.Request.Host = new HostString("jdservices.local.test");

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetConfiguration(Guid.Parse("848e9f13-07c1-41ae-baef-57bb9f4ac899")));
        ModernJduApplicationConfigurationResponse response =
            Assert.IsType<ModernJduApplicationConfigurationResponse>(result.Value);

        Assert.Equal(Guid.Parse("848e9f13-07c1-41ae-baef-57bb9f4ac899"), response.Configuration.PlatformConfig.ApplicationId);
        Assert.Equal(Guid.Parse("e92e485b-893e-4dd1-a55d-13d2696fcc38"), response.Configuration.PlatformConfig.SpaceId);
        Assert.Equal("NX", response.Configuration.PlatformConfig.Platform);
        Assert.Contains(response.Configuration.FeaturesSwitches, item =>
            item.Name == "CreateSession" && item.Value);
        Assert.Contains(response.Configuration.GatewayResources, item => item.Name == "populations");
        Assert.Contains(response.Configuration.GatewayResources, item => item.Name == "spaces/entities");
        Assert.All(response.Configuration.GatewayResources, item =>
            Assert.Contains("jdservices.local.test", item.Url, StringComparison.Ordinal));
        Assert.DoesNotContain(response.Configuration.GatewayResources, item =>
            item.Url.Contains("ubiservices.ubi.com", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Jdu2019WiiURoute_ReturnsCapturedApplicationAndSpaceIdentity()
    {
        Jdu2019ConfigurationController controller = new(new ModernJduApplicationConfigurationService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Host = new HostString("jdservices.local.test");

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetConfiguration(Guid.Parse("3b4009a1-66cb-410d-9799-3041835d8e1b")));
        ModernJduApplicationConfigurationResponse response =
            Assert.IsType<ModernJduApplicationConfigurationResponse>(result.Value);

        Assert.Equal(Guid.Parse("3b4009a1-66cb-410d-9799-3041835d8e1b"), response.Configuration.PlatformConfig.ApplicationId);
        Assert.Equal(Guid.Parse("ef680ba3-e1ae-4fb5-b434-ab448f1c3210"), response.Configuration.PlatformConfig.SpaceId);
        Assert.Equal("WIIU", response.Configuration.PlatformConfig.Platform);
    }

    [Fact]
    public void Jdu2019ConfigurationRoute_UsesApplicationIdParameter()
    {
        HttpGetAttribute route = Assert.Single(typeof(Jdu2019ConfigurationController)
            .GetMethod(nameof(Jdu2019ConfigurationController.GetConfiguration))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>());

        Assert.Equal("{applicationId:guid}/configuration", route.Template);
    }

    [Fact]
    public void Jdu2019Entities_ReturnConfiguredJmcsHostForCapturedSpace()
    {
        JduEntitiesController controller = new(
            new ClassicJduDiscoveryService(LocalUrls),
            new ModernJduDiscoveryService(LocalUrls));

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetEntities(Guid.Parse("e92e485b-893e-4dd1-a55d-13d2696fcc38")));
        JsonElement response = JsonSerializer.SerializeToElement(
            result.Value,
            JsonSerializerOptions.Web);
        JsonElement entity = Assert.Single(response.GetProperty("entities").EnumerateArray());

        Assert.Equal(Jdu2019ClientProfiles.ServerEntityId, entity.GetProperty("entityId").GetGuid());
        Assert.Equal(Guid.Parse("e92e485b-893e-4dd1-a55d-13d2696fcc38"), entity.GetProperty("spaceId").GetGuid());
        Assert.Equal("server", entity.GetProperty("type").GetString());
        Assert.Equal("default", entity.GetProperty("name").GetString());
        Assert.Equal("JMCS PROD", entity.GetProperty("obj").GetProperty("name").GetString());
        Assert.Equal("https://jdu.local.test", entity.GetProperty("obj").GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("e92e485b-893e-4dd1-a55d-13d2696fcc38")]
    [InlineData("ef680ba3-e1ae-4fb5-b434-ab448f1c3210")]
    public void Jdu2019PopulationGet_ReturnsCapturedRemoteLogAssignment(string spaceIdText)
    {
        Guid spaceId = Guid.Parse(spaceIdText);
        PopulationsController controller = new(new ClassicJduDiscoveryService(LocalUrls));

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetPopulations(spaceId.ToString()));
        JsonElement response = JsonSerializer.SerializeToElement(result.Value);
        JsonElement population = Assert.Single(response.GetProperty("populations").EnumerateArray());

        Assert.Equal("WithoutLogs", population.GetProperty("name").GetString());
        Assert.Equal("RemoteLogs", population.GetProperty("subject").GetString());
        Assert.Equal(spaceId, population.GetProperty("spaceId").GetGuid());
    }

    [Fact]
    public async Task ProfilesProbe_WithoutLookupParameters_IsSuccessfulAndEmpty()
    {
        UbisoftProfilesController controller = new(Mock.Of<IPlayerIdentityService>());

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            await controller.GetProfiles(null, null, null, null));
        JsonElement response = JsonSerializer.SerializeToElement(
            result.Value,
            JsonSerializerOptions.Web);

        Assert.Empty(response.GetProperty("profiles").EnumerateArray());
    }

    [Fact]
    public async Task ProfilesLookup_AcceptsGuidPlatformIdentifier()
    {
        Guid profileId = Guid.NewGuid();
        Mock<IPlayerIdentityService> identityService = new();
        identityService
            .Setup(service => service.GetProfilesByPlatformIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                "uplay"))
            .ReturnsAsync([
                new LinkedProfileIdentity(profileId, profileId, "uplay", profileId.ToString(), "Test Dancer")
            ]);
        UbisoftProfilesController controller = new(identityService.Object);

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            await controller.GetProfiles(null, null, profileId.ToString(), "uplay"));
        JsonElement response = JsonSerializer.SerializeToElement(
            result.Value,
            JsonSerializerOptions.Web);
        JsonElement returnedProfile = Assert.Single(response.GetProperty("profiles").EnumerateArray());

        Assert.Equal(profileId, returnedProfile.GetProperty("profileId").GetGuid());
        Assert.Equal("uplay", returnedProfile.GetProperty("platformType").GetString());
        Assert.Equal("Test Dancer", returnedProfile.GetProperty("nameOnPlatform").GetString());
    }

	[Fact]
	public async Task ProfilesLookup_AcceptsSdkProfileIdListSelector()
	{
		Guid firstId = Guid.NewGuid();
		Guid secondId = Guid.NewGuid();
		Mock<IPlayerIdentityService> identityService = new();
		identityService.Setup(service => service.GetProfilesByIdsAsync(
			It.IsAny<IEnumerable<Guid>>(),
			"switch"))
			.ReturnsAsync([
				new LinkedProfileIdentity(firstId, firstId, "switch", "first", "First"),
				new LinkedProfileIdentity(secondId, secondId, "switch", "second", "Second")
			]);
		UbisoftProfilesController controller = new(identityService.Object);

		OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetProfiles(
			null,
			null,
			null,
			"switch",
			$"{firstId},{secondId}"));
		JsonElement response = JsonSerializer.SerializeToElement(result.Value);

		Assert.Equal(2, response.GetProperty("profiles").GetArrayLength());
		identityService.Verify(service => service.GetProfilesByIdsAsync(
			It.Is<IEnumerable<Guid>>(ids => ids.Contains(firstId) && ids.Contains(secondId)),
			"switch"), Times.Once);
	}
}
