using Microsoft.AspNetCore.Mvc;

using SpaceParametersController = JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v2.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.Parameters;

namespace JustDanceNextPlus.Tests.Controllers;

public class SpaceParametersControllerTests
{
    [Fact]
    public void SpaceParametersController_ExposesPcDebugBuildRoute()
    {
        string[] routeTemplates = typeof(SpaceParametersController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray()!;

        Assert.Contains("/v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/parameters", routeTemplates);
        Assert.Contains("/v2/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/parameters", routeTemplates);

        HttpGetAttribute httpGet = Assert.Single(typeof(SpaceParametersController)
            .GetMethod(nameof(SpaceParametersController.GetParametersSpaces))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
            .Cast<HttpGetAttribute>());
        Assert.Null(httpGet.Name);
    }
}
