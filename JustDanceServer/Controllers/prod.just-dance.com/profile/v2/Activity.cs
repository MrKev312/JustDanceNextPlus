using Microsoft.AspNetCore.Mvc;

using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

namespace JustDanceServer.Controllers.prod.just_dance.com.profile.v2;

[ApiController]
[Route("profile/v2")]
public sealed class Activity(IPlayerActivityService activityService, ISessionManager sessions) : ControllerBase
{
    [HttpGet("challenges/{mapName}")]
    public IActionResult GetChallenge([FromRoute] string mapName)
        => Ok(new { ugcId = string.Empty });

    [HttpPost("map-ended")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> MapEnded([FromBody] JduMapEndedEntry[] history)
    {
        PlayerRequestContext context = PlayerRequestContextResolver.FromRequest(Request, sessions);
        if (context.ProfileId is null)
            return Unauthorized();
        if (!context.SupportsMapHistory)
            return NotFound();
        return await activityService.RecordJduHistoryAsync(context, history) ? Ok() : BadRequest();
    }
}
