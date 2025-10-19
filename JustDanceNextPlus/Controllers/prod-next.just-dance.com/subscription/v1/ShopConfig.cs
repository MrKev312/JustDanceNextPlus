using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.subscription.v1;

[ApiController]
[Route("subscription/v1/shop-config")]
public class ShopConfig(IBundleService bundleService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetShopConfig()
	{
		return Ok(bundleService.ShopConfig);
	}
}
