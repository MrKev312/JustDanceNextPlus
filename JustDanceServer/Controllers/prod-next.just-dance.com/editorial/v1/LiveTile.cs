using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/live-tile")]
public class LiveTile(IBundleService bundleService) : ControllerBase
{
    [HttpGet(Name = "GetLiveTile")]
    public IActionResult GetLiveTile()
    {
        string platform = ClientPlatform.FromRequest(Request);
        return Ok(bundleService.GetLiveTiles(platform));
    }
}
