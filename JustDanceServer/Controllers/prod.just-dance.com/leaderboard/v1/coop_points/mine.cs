using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.leaderboard.v1.coop_points;

[ApiController]
[Route("leaderboard/v1/coop_points/mine")]
public sealed class Mine : ControllerBase
{
    [HttpGet]
    public IActionResult GetCoopPoints()
    {
        return Ok(new { });
    }
}
