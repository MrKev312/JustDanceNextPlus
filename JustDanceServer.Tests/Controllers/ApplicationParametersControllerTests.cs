using Microsoft.AspNetCore.Mvc;

using ApplicationParametersController = JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.applications.guid.Parameters;

namespace JustDanceServer.Tests.Controllers;

public class ApplicationParametersControllerTests
{
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
}
