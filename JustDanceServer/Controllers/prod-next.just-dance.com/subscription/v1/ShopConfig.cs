using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.subscription.v1;

[ApiController]
[Route("subscription/v1/shop-config")]
public class ShopConfig(IBundleService bundleService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetShopConfig()
    {
        string platform = ClientPlatform.FromRequest(Request);
        return Ok(bundleService.GetShopConfig(platform));
    }
}
