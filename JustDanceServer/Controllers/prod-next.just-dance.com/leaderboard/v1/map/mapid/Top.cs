using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.leaderboard.v1.map.mapid;

[ApiController]
[Route("leaderboard/v1/map/{mapId:guid}/top")]
public class Top(IUserDataService userDataService, IMapService mapService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromRoute] Guid mapId, [FromQuery] int limit = 10, [FromQuery] int skip = 0)
    {
        if (!mapService.TryGetMapName(mapId, out string mapName))
            return NotFound();

        Leaderboard leaderboard = await userDataService.GetLeaderboardAsync(mapName, limit, skip);

        // Return the leaderboard
        return Ok(leaderboard);
    }
}
