using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.subscription.v1;

[ApiController]
[Route("subscription/v1/shop-config")]
public class ShopConfig(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetShopConfig()
	{
		string shopConfigJson = Path.Combine(pathSettings.Value.JsonsPath, "shop-config.json");

		return Ok(System.IO.File.ReadAllText(shopConfigJson));
	}
}
