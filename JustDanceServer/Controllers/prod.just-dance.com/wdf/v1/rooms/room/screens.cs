using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/screens")]
public class Screens(IWdfService wdfService, ISessionManager sessionManager) : ControllerBase
{
    [HttpPost]
    public IActionResult GetScreens(
        [FromRoute] string room,
        [FromBody] WdfScreensRequest? request)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
            return BadRequest();

        Guid? playerId = RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session)
            ? session.PlayerId
            : null;
        return Ok(wdfService.GetScreens(resolvedRoom, request?.StartTime, playerId));
    }
}

public sealed record WdfScreensRequest
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double StartTime { get; init; }
}
