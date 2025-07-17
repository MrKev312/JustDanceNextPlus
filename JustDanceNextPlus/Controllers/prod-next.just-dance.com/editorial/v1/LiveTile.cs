using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/live-tile")]
public class LiveTile(BundleService bundleService) : ControllerBase
{
	[HttpGet(Name = "GetLiveTile")]
	public IActionResult GetLiveTile()
	{
		return Ok(bundleService.LiveTiles);
	}
}
