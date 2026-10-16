using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Json;

using ApplicationParametersController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.applications.guid.Parameters;

namespace JustDanceServer.Tests.Controllers;

public class ApplicationParametersControllerTests
{
    private static readonly IOptions<UrlSettings> LocalUrls = Options.Create(new UrlSettings
    {
        HostUrl = "next.local.test",
        JduHostUrl = "jdu.local.test",
        CDNUrl = "next.local.test"
    });

    [Fact]
    public void ApplicationParametersController_ExposesPcDebugBuildRoute()
    {
        string[] routeTemplates = typeof(ApplicationParametersController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray()!;

        Assert.Contains("/v1/applications/{Guid:guid}/parameters", routeTemplates);
        Assert.Contains("/v2/applications/{Guid:guid}/parameters", routeTemplates);

        HttpGetAttribute httpGet = Assert.Single(typeof(ApplicationParametersController)
            .GetMethod(nameof(ApplicationParametersController.GetParameters))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>());
        Assert.Null(httpGet.Name);
    }

    [Theory]
    [MemberData(nameof(JdNextApplicationIds))]
    public void ApplicationParameters_ResolveKnownJdNextApplication(string applicationId)
    {
        ApplicationParametersController controller = CreateController();

        OkObjectResult result = Assert.IsType<OkObjectResult>(
            controller.GetParameters(Guid.Parse(applicationId)));
        ParameterList parameters = Assert.IsType<ParameterList>(result.Value);

        Assert.Equal(
            JdNextConstants.GameSpaceId,
            parameters.Parameters["us-staging"].Fields["spaceId"]);
        Dictionary<string, string> aws = Assert.IsType<Dictionary<string, string>>(
            parameters.Parameters["us-sdkClientUrlsPlaceholders"].Fields["baseurl_aws"]);
        Assert.Equal("https://{env}next.local.test", aws["Standard"]);
    }

    public static TheoryData<string> JdNextApplicationIds =>
    [
        JdNextConstants.PcApplicationId.ToString(),
        JdNextConstants.SwitchApplicationId.ToString()
    ];

    [Fact]
    public void NewBuildKeepsLegacyApplicationDiscoveryUrlsAndTlsConfiguration()
    {
        ApplicationParametersController legacy = CreateController();
        ApplicationParametersController modern = CreateController();
        modern.Request.Headers["ubi-appbuildid"] = "live_27.0.1-417080_NX_Final_only-gel_260916";
        var before = Assert.IsType<OkObjectResult>(legacy.GetParameters(JdNextConstants.SwitchApplicationId));
        var after = Assert.IsType<OkObjectResult>(modern.GetParameters(JdNextConstants.SwitchApplicationId));
        Assert.Equal(JsonSerializer.Serialize(before.Value), JsonSerializer.Serialize(after.Value));
    }

    [Fact]
    public void ApplicationParameters_RejectUnknownApplication()
    {
        ApplicationParametersController controller = CreateController();

        Assert.IsType<NotFoundResult>(controller.GetParameters(Guid.NewGuid()));
    }

    private static ApplicationParametersController CreateController()
    {
        return new ApplicationParametersController(LocalUrls)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
