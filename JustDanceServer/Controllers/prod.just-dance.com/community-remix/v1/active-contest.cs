using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.community_remix.v1;

[ApiController]
[Route("community-remix/v1/active-contest")]
public sealed class ActiveContest : ControllerBase
{
    [HttpGet]
    public IActionResult GetActiveContest()
        => Ok(new { id = 0, mapName = string.Empty });
}
