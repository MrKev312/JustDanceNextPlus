using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
