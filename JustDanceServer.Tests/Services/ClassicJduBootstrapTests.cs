using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json.Nodes;

using ClassicActionsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.Actions;
using ClassicActiveContestController = JustDanceServer.Controllers.prod.just_dance.com.community_remix.v1.ActiveContest;
using ClassicApplicationsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.Applications;
using ClassicAvatarsController = JustDanceServer.Controllers.prod.just_dance.com.avatardb.v1.Avatars;
using ClassicConfigurationController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.applications.Configuration;
using ClassicProfilesController = JustDanceServer.Controllers.prod.just_dance.com.profile.v2.Profiles;
using ClassicRewardsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.Rewards;
using JduEntitiesController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces.JduEntities;
using NewsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.News;

namespace JustDanceServer.Tests.Services;
public sealed class ClassicJduBootstrapTests
{
	private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
	};

	[Fact]
	public void ClientProfiles_ContainAllKnownClassicPlatforms()
	{
		Assert.Equal(14, ClassicJduClientProfiles.All.Count);
		Assert.True(ClassicJduClientProfiles.TryGetByApplicationId(
			Guid.Parse("3696a38b-1edd-4e7b-bbb7-855f15a25408"),
			out ClassicJduClientProfile? jdu2016WiiU));
		Assert.Equal(Guid.Parse("17599f67-8a3b-407d-ac5e-7dfdd66d5194"), jdu2016WiiU.SpaceId);
		Assert.True(ClassicJduClientProfiles.TryGetByApplicationId(
			Guid.Parse("81cf5bb0-e037-4374-b38d-106bd21307fa"),
			out ClassicJduClientProfile? jdu2017WiiU));
		Assert.Equal(Guid.Parse("96f82187-4d45-4544-9718-0cde2f8d5ccb"), jdu2017WiiU.SpaceId);
		Assert.True(ClassicJduClientProfiles.TryGetByApplicationId(
			Guid.Parse("613fe1a7-86c6-4fa7-bfb3-230e92ae29d3"),
			out ClassicJduClientProfile? jdu2017Switch));
		Assert.Equal(Guid.Parse("0415144b-ed4c-4098-a50d-5ba36d7d8466"), jdu2017Switch.SpaceId);
		Assert.True(ClassicJduClientProfiles.TryGetByApplicationId(
			Guid.Parse("853badc5-e5b8-47a9-be06-630a1071701c"),
			out ClassicJduClientProfile? jdu2018Switch));
		Assert.Equal(Guid.Parse("cdf9d0d7-8e4d-4afa-8268-c25ad42c579b"), jdu2018Switch.SpaceId);
		Assert.Contains(
			ClassicJduClientProfiles.All,
			profile => profile.ApplicationId == Guid.Parse("740a6dc8-7d7a-4fbe-be2c-aa5d8c65c5e8")
				&& profile.AssetPlatform == "PC");
		Assert.Contains(
			ClassicJduClientProfiles.All,
			profile => profile.ApplicationId == Guid.Parse("f8c1989f-ea92-4567-b43d-436de74b2c5d")
				&& profile.AssetPlatform == "WIIU");
		Assert.Contains(
			ClassicJduClientProfiles.All,
			profile => profile.ApplicationId == Guid.Parse("338f2a70-2587-44ee-a64b-5dbe112fb043")
				&& profile.AssetPlatform == "ORBIS");
		Assert.Contains(
			ClassicJduClientProfiles.All,
			profile => profile.ApplicationId == Guid.Parse("ccfda907-9767-4ae5-895c-6204e7a39cbd")
				&& profile.AssetPlatform == "DURANGO");
		Assert.True(JduMediaClientProfiles.TryResolve("jd2017-nx-all", out JduMediaClientProfile? nx2017));
		Assert.Equal(JduPlatform.NintendoSwitch, nx2017.Platform);
		Assert.Equal(MapVideoCodec.Vp8, nx2017.VideoCodec);
		Assert.True(JduMediaClientProfiles.TryResolve("jd2018-nx-all", out JduMediaClientProfile? nx2018));
		Assert.Equal(MapVideoCodec.Vp9, nx2018.VideoCodec);
		Assert.True(JduMediaClientProfiles.TryResolve("jd2018-wiiu-noe", out JduMediaClientProfile? wiiU2018));
		Assert.Equal(JduPlatform.WiiU, wiiU2018.Platform);
		Assert.Equal(MapVideoCodec.Vp8, wiiU2018.VideoCodec);
		Assert.True(JduMediaClientProfiles.TryResolve("jd2022-nx-all", out JduMediaClientProfile? nx2022));
		Assert.Equal(MapVideoCodec.Vp9, nx2022.VideoCodec);
		Assert.True(JduMediaClientProfiles.TryResolve("jd2022-ps5-all", out JduMediaClientProfile? ps52022));
		Assert.Equal(MapVideoCodec.Vp9, ps52022.VideoCodec);
		Assert.True(JduMediaClientProfiles.TryResolve("jd2022-ps4-all", out JduMediaClientProfile? ps42022));
		Assert.Equal(MapVideoCodec.Vp8, ps42022.VideoCodec);
		Assert.All(
			new[]
			{
				JduPlatform.Pc,
				JduPlatform.NintendoSwitch,
				JduPlatform.PlayStation4,
				JduPlatform.PlayStation5,
				JduPlatform.XboxOne
			},
			platform => Assert.True(JduVideoCapabilities.Supports1080p(platform)));
		Assert.False(JduVideoCapabilities.Supports1080p(JduPlatform.WiiU));
	}

	[Fact]
	public void Configuration_UsesTypedProfileAndSourceRequiredDiscoveryResources()
	{
		IClassicJduConfigurationService service = new ClassicJduConfigurationService();
		ClassicJduConfigurationResponse response = service.Create(
			ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch),
			"https://jdservices.local.test");

		Assert.Equal(Guid.Parse("853badc5-e5b8-47a9-be06-630a1071701c"), response.Configuration.PlatformConfig.ApplicationId);
		Assert.Equal(Guid.Parse("cdf9d0d7-8e4d-4afa-8268-c25ad42c579b"), response.Configuration.PlatformConfig.SpaceId);
		Assert.Equal("SWITCH", response.Configuration.PlatformConfig.Platform);
		Assert.Equal(
			["FriendsLookup", "FriendsRequest", "Profiles", "UplayFriends", "Users"],
			response.Configuration.FeaturesSwitches.Select(feature => feature.Name));
		Assert.All(response.Configuration.FeaturesSwitches, feature => Assert.True(feature.Value));
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "friends"
			&& resource.Url.EndsWith("/{version}/profiles/me/friends", StringComparison.Ordinal)
			&& resource.Version == 2);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "profiles"
			&& resource.Url.EndsWith("/{version}/profiles", StringComparison.Ordinal)
			&& resource.Version == 2);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "populations" && resource.Version == 1);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "spaces/entities" && resource.Version == 2);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "news" && resource.Version == 1);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "profiles/applications"
			&& resource.Url.EndsWith("/{version}/profiles/me/applications", StringComparison.Ordinal)
			&& resource.Version == 1);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "profiles/actions"
			&& resource.Url.EndsWith("/{version}/profiles/{profileId}/actions", StringComparison.Ordinal)
			&& resource.Version == 1);
		Assert.Contains(response.Configuration.GatewayResources, resource =>
			resource.Name == "profiles/rewards"
			&& resource.Url.EndsWith("/{version}/profiles/{profileId}/rewards", StringComparison.Ordinal)
			&& resource.Version == 1);
		Assert.All(response.Configuration.GatewayResources, resource =>
			Assert.Contains("jdservices.local.test", resource.Url, StringComparison.Ordinal));
		Assert.DoesNotContain(response.Configuration.GatewayResources, resource =>
			resource.Url.Contains("ubiservices.ubi.com", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Jdu2016BootstrapEndpoints_ReturnClientExpectedEmptyShapes()
	{
		Guid profileId = Guid.NewGuid();
		Assert.Empty(GetProperty(
			Assert.IsType<OkObjectResult>(new ClassicApplicationsController().GetApplications()).Value,
			"applications").EnumerateArray());
		Assert.Empty(GetProperty(
			Assert.IsType<OkObjectResult>(new ClassicActionsController().GetActions(profileId)).Value,
			"actions").EnumerateArray());
		Assert.Empty(GetProperty(
			Assert.IsType<OkObjectResult>(new ClassicRewardsController().GetRewards(profileId)).Value,
			"rewards").EnumerateArray());

		Mock<IJduCatalogService> catalogService = new();
		catalogService
			.Setup(service => service.TryCreateLegacyAvatarCatalog(
				It.IsAny<string?>(),
				out It.Ref<JsonObject>.IsAny))
			.Returns(false);
		JsonObject avatars = Assert.IsType<JsonObject>(
			Assert.IsType<OkObjectResult>(
				new ClassicAvatarsController(catalogService.Object).GetAvatars(null)).Value);
		Assert.Equal("OnlineAvatarDb", avatars["__class"]!.GetValue<string>());
		Assert.Empty(avatars["avatars"]!.AsObject());

		OkObjectResult contestResult = Assert.IsType<OkObjectResult>(
			new ClassicActiveContestController().GetActiveContest());
		System.Text.Json.JsonElement contest =
			System.Text.Json.JsonSerializer.SerializeToElement(contestResult.Value);
		Assert.Equal(0, contest.GetProperty("id").GetInt32());
		Assert.Equal(string.Empty, contest.GetProperty("mapName").GetString());

		RouteAttribute[] profileRoutes = typeof(ClassicProfilesController)
			.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
			.Cast<RouteAttribute>()
			.ToArray();
		Assert.Contains(profileRoutes, route => route.Template == "profile/v1/profiles");
		Assert.Contains(profileRoutes, route => route.Template == "profile/v2/profiles");
	}

	[Fact]
	public void ConfigurationRoute_ResolvesEveryClassicApplicationThroughOneTemplate()
	{
		ClassicConfigurationController controller = new(new ClassicJduConfigurationService())
		{
			ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
		};
		controller.Request.Host = new HostString("jdservices.local.test");

		HttpGetAttribute route = Assert.Single(typeof(ClassicConfigurationController)
			.GetMethod(nameof(ClassicConfigurationController.GetConfiguration))!
			.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
			.Cast<HttpGetAttribute>());
		Assert.Equal("{applicationId:guid}/configuration", route.Template);

		foreach (ClassicJduClientProfile profile in ClassicJduClientProfiles.All)
		{
			OkObjectResult result = Assert.IsType<OkObjectResult>(
				controller.GetConfiguration(profile.ApplicationId));
			ClassicJduConfigurationResponse response =
				Assert.IsType<ClassicJduConfigurationResponse>(result.Value);
			Assert.Equal(profile.ApplicationId, response.Configuration.PlatformConfig.ApplicationId);
		}

		Assert.IsType<NotFoundResult>(controller.GetConfiguration(Guid.NewGuid()));
	}

	[Fact]
	public void EntityRoute_ResolvesEveryJduSpaceThroughOneTemplate()
	{
		IOptions<UrlSettings> urls = Options.Create(new UrlSettings
		{
			HostUrl = "next.local.test",
			JduHostUrl = "jdu.local.test",
			CDNUrl = "next.local.test"
		});
		JduEntitiesController controller = new(
			new ClassicJduDiscoveryService(urls),
			new ModernJduDiscoveryService(urls));

		HttpGetAttribute route = Assert.Single(typeof(JduEntitiesController)
			.GetMethod(nameof(JduEntitiesController.GetEntities))!
			.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
			.Cast<HttpGetAttribute>());
		Assert.Equal("{spaceId:guid}/entities", route.Template);

		Assert.All(ClassicJduClientProfiles.All, profile =>
			Assert.IsType<OkObjectResult>(controller.GetEntities(profile.SpaceId)));
		Assert.All(ModernJduClientProfiles.All, profile =>
			Assert.IsType<OkObjectResult>(controller.GetEntities(profile.SpaceId)));
		Assert.IsType<NotFoundResult>(controller.GetEntities(Guid.NewGuid()));
	}

	[Fact]
	public void News_ResolvesSpaceFromApplicationInsteadOfDefaultingToAPlatform()
	{
		Mock<IEditorialService> editorial = new();
		editorial.Setup(service => service.GetEntries(It.IsAny<EditorialAudience>()))
			.Returns(
			[
				new EditorialEntry
				{
					Id = Guid.NewGuid(),
					Title = "this is a test",
					Body = "this is a test"
				}
			]);
		NewsController controller = new(editorial.Object)
		{
			ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
		};
		controller.Request.Headers["Ubi-AppId"] = Guid.Parse("81cf5bb0-e037-4374-b38d-106bd21307fa").ToString();

		OkObjectResult result = Assert.IsType<OkObjectResult>(controller.GetNews(null));
		System.Text.Json.JsonElement response = System.Text.Json.JsonSerializer.SerializeToElement(
			result.Value,
			SerializerOptions);
		System.Text.Json.JsonElement news = Assert.Single(response.GetProperty("news").EnumerateArray());
		Assert.Equal(Guid.Parse("96f82187-4d45-4544-9718-0cde2f8d5ccb"), news.GetProperty("spaceId").GetGuid());
	}

	[Fact]
	public void Discovery_ProvidesValidPopulationAndEntityFallbackForSameSpace()
	{
		ClassicJduDiscoveryService service = new(Options.Create(new UrlSettings
		{
			JduHostUrl = "jdu.local.test"
		}));

		ClassicJduPopulation population = service.CreatePopulation(ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));
		ClassicJduEntity entity = service.CreateEntity(ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));
		Assert.Equal("server", population.Subject);
		Assert.Equal(Guid.Parse("cdf9d0d7-8e4d-4afa-8268-c25ad42c579b"), population.SpaceId);
		Assert.Equal("https://jdu.local.test", population.Obj.Url);
		Assert.Equal(JduConstants.ClassicServerEntityId, entity.EntityId);
		Assert.Equal("qc", entity.Name);
		Assert.Equal("server", entity.Type);
		Assert.Equal(population.Obj.Url, entity.Obj.Url);
	}

	private static System.Text.Json.JsonElement GetProperty(object? value, string propertyName)
		=> System.Text.Json.JsonSerializer.SerializeToElement(value).GetProperty(propertyName);
}
