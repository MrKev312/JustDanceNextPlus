using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json;

using ApplicationParametersController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.applications.guid.Parameters;
using JduEntitiesController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces.JduEntities;
using JduSessionController = JustDanceServer.Controllers.prod.just_dance.com.sessions.v1.Session;
using JduSpaceParametersController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._041c03fa_1735_4ea7_b5fc_c16546d092ca.Parameters;
using PopulationDataController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.populations.Data;
using PopulationsController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.Populations;

namespace JustDanceServer.Tests.Controllers;

public sealed class Jdu2022DiscoveryTests
{
    private static readonly IOptions<UrlSettings> LocalUrls = Options.Create(new UrlSettings
    {
        HostUrl = "next.local.test",
        JduHostUrl = "jdu.local.test",
        CDNUrl = "next.local.test"
    });

    [Fact]
    public void ApplicationParameters_KeepJduUbisoftTrafficOnDiscoveryHost()
    {
        ApplicationParametersController controller = new(LocalUrls)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Host = new HostString("public.local.test");

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetParameters(Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d")));
        ParameterList parameters = Assert.IsType<ParameterList>(result.Value);

        Assert.Equal(
            Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca"),
            parameters.Parameters["us-staging"].Fields["spaceId"]);
        Dictionary<string, string> aws = Assert.IsType<Dictionary<string, string>>(
            parameters.Parameters["us-sdkClientUrlsPlaceholders"].Fields["baseurl_aws"]);
        Assert.Equal("https://{env}public.local.test", aws["Standard"]);
    }

    [Fact]
    public void SpaceParameters_PointJmcsAtConfiguredJduHost()
    {
        JduSpaceParametersController controller = new(LocalUrls);

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetParameters(Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca")));
        ParameterList parameters = Assert.IsType<ParameterList>(result.Value);

        Assert.Equal("https://jdu.local.test", parameters.Parameters["jd-serverInfo"].Fields["url"]);
        Assert.Equal("PROD", parameters.Parameters["jd-serverInfo"].Fields["name"]);
        Assert.Equal(
            UbisoftServicesConstants.SocialSpaceId,
            parameters.Parameters["us-GlobalSpaceConfig"].Fields["friendsSpaceId"]);
        Assert.Equal(
            UbisoftServicesConstants.SocialSpaceId,
            parameters.Parameters["us-GlobalSpaceConfig"].Fields["blocklistSpaceId"]);
    }

    [Theory]
    [InlineData(2021)]
    [InlineData(2022)]
    public void EveryPlatformDiscoversItsSharedGameSpace(int gameYear)
    {
        Guid gameSpaceId = gameYear == 2021 ? Guid.Parse("59f82720-778f-427a-a0bd-e81cb112c044") : Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca");
        foreach (ModernJduClientProfile client in ModernJduClientProfiles.All.Where(client => client.GameYear == gameYear))
        {
            ApplicationParametersController controller = new(LocalUrls)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            controller.Request.Host = new HostString("public.local.test");

            ParameterList parameters = Assert.IsType<ParameterList>(
                Assert.IsType<OkObjectResult>(controller.GetParameters(client.ApplicationId)).Value);
            Assert.Equal(gameSpaceId, parameters.Parameters["us-staging"].Fields["spaceId"]);
            Dictionary<string, string> aws = Assert.IsType<Dictionary<string, string>>(
                parameters.Parameters["us-sdkClientUrlsPlaceholders"].Fields["baseurl_aws"]);
            Assert.Equal("https://{env}public.local.test", aws["Standard"]);

            PopulationDataController populationData = new();
            JsonElement body = JsonSerializer.SerializeToElement(new { spaceId = client.SpaceId });
            JsonElement response = JsonSerializer.SerializeToElement(
                Assert.IsType<OkObjectResult>(populationData.PutPopulationsData(body)).Value);
            Assert.Empty(response.GetProperty("populations").EnumerateArray());
        }
    }

    [Theory]
    [InlineData(2021, 1)]
    [InlineData(2022, 13)]
    public void SharedGameSpacesRemainDiscoverableWithoutSelectingAPlatform(int gameYear, int populationCount)
    {
        Guid gameSpaceId = gameYear == 2021 ? Guid.Parse("59f82720-778f-427a-a0bd-e81cb112c044") : Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca");
        JduEntitiesController entities = new(
            new ClassicJduDiscoveryService(LocalUrls),
            new ModernJduDiscoveryService(LocalUrls));
        JsonElement entityResponse = JsonSerializer.SerializeToElement(
            Assert.IsType<OkObjectResult>(entities.GetEntities(gameSpaceId)).Value,
            JsonSerializerOptions.Web);
        Assert.Equal(gameSpaceId,
            Assert.Single(entityResponse.GetProperty("entities").EnumerateArray()).GetProperty("spaceId").GetGuid());

        PopulationsController populations = new(new ClassicJduDiscoveryService(LocalUrls));
        JsonElement populationResponse = JsonSerializer.SerializeToElement(
            Assert.IsType<OkObjectResult>(populations.GetPopulations(gameSpaceId.ToString())).Value);
        Assert.Equal(populationCount, populationResponse.GetProperty("populations").GetArrayLength());
        Assert.All(populationResponse.GetProperty("populations").EnumerateArray(),
            population => Assert.Equal(gameSpaceId, population.GetProperty("spaceId").GetGuid()));

        PopulationDataController populationData = new();
        JsonElement body = JsonSerializer.SerializeToElement(new { spaceId = gameSpaceId });
        JsonElement putResponse = JsonSerializer.SerializeToElement(
            Assert.IsType<OkObjectResult>(populationData.PutPopulationsData(body)).Value);
        Assert.Equal(populationCount, putResponse.GetProperty("populations").GetArrayLength());
        Assert.All(putResponse.GetProperty("populations").EnumerateArray(),
            population => Assert.Equal(gameSpaceId, population.GetProperty("spaceId").GetGuid()));
    }

    [Fact]
    public void PopulationPut_ReturnsCapturedJduAssignments()
    {
        PopulationDataController controller = new();
        string alternateGuidFormat = Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca").ToString("B").ToUpperInvariant();
        using JsonDocument body = JsonDocument.Parse($$"""
            {
              "spaceId": "{{alternateGuidFormat}}",
              "data": {}
            }
            """);

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.PutPopulationsData(body.RootElement));
        JsonElement response = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(13, response.GetProperty("populations").GetArrayLength());
    }

    [Fact]
    public void PopulationGet_ReturnsAssignmentsForJduSpace()
    {
        PopulationsController controller = new(new ClassicJduDiscoveryService(LocalUrls));

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetPopulations(Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca").ToString()));
        JsonElement response = JsonSerializer.SerializeToElement(result.Value);

        Assert.Equal(13, response.GetProperty("populations").GetArrayLength());
    }

    [Fact]
    public void JmcsSession_CreatesPairingFromClientRequest()
    {
        Guid sessionId = Guid.NewGuid();
        Session? session = new()
        {
            SessionId = sessionId,
            PlayerId = Guid.NewGuid(),
            UbiAppId = Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d")
        };
        Mock<ISessionManager> sessionManager = new();
        sessionManager.Setup(service => service.TryGetSessionById(sessionId, out session))
            .Returns(true);
        Mock<IPairingService> pairingService = new();
        pairingService.Setup(service => service.CreatePairing(
            "192.168.1.20", 443, string.Empty, "certificate",
            "v2.phonescoring.jd.ubisoft.com", "title", "Console", 38, 6))
            .Returns(new PairingEntry
            {
                PairingCode = "123456",
                DeviceIp = "192.168.1.20",
                DevicePort = 443,
                TlsCertificate = "certificate"
            });
        JduSessionController controller = new(sessionManager.Object, pairingService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.Request.Headers["ubi-sessionid"] = sessionId.ToString();

        OkObjectResult result = Assert.IsType<OkObjectResult>(controller.CreateSession(new()
        {
            PublicIp = "192.168.1.20",
            PairingInfo = new()
            {
                PairingUrl = "https://192.168.1.20",
                TlsCertificate = "certificate",
                TitleId = "title",
                DisplayName = "Console"
            }
        }));
        JustDanceServer.Controllers.prod.just_dance.com.sessions.v1.SessionResponse response = Assert.IsType<JustDanceServer.Controllers.prod.just_dance.com.sessions.v1.SessionResponse>(result.Value);

        Assert.Equal(6, response.PairingCode.Length);
        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(40, response.DocId.Length);
    }
}
