using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.leaderboard.v1;

[ApiController]
[Route("leaderboard/v1/gacha")]
public sealed class Gacha(
    IProgressionService progressionService,
    ISessionManager sessionManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();

        ProgressionSnapshot progression = await progressionService.GetAsync(
            session.PlayerId,
            ProgressionFamily.JDUnlimited);
        return Ok(new { rank = progression.GachaRank });
    }
}
