using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room.themes.vote;

[ApiController]
[Route("wdf/v1/rooms/{room}/themes/vote")]
public sealed class Vote(IWdfService wdfService, ISessionManager sessionManager) : ControllerBase
{
    [HttpPost("choice")]
    public IActionResult Choose(
        [FromRoute] string room,
        [FromBody] WdfVoteChoiceRequest request)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();

        return wdfService.SubmitVote(resolvedRoom, session.PlayerId, request.VoteOption)
            ? Ok()
            : BadRequest();
    }

    [HttpGet("result")]
    public IActionResult Result([FromRoute] string room)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();

        if (RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            wdfService.Touch(resolvedRoom, session.PlayerId);

        return Ok(wdfService.GetVoteResult(resolvedRoom));
    }
}

public sealed record WdfVoteChoiceRequest(string VoteOption);
