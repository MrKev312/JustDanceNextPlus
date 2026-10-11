using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1;

[ApiController]
[Route("wdf/v1/assign-room")]
public class AssignRoom(IWdfService wdfService, ISessionManager sessionManager) : ControllerBase
{
    [HttpPost]
    public IActionResult Assign()
    {
        if (!WdfRequestResolver.TryGetGameYear(Request, out int gameYear))
            return BadRequest("Unsupported or missing 'X-SkuId' header.");

        WdfRoomId room = wdfService.GetAssignedRoom(gameYear);
        if (RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            wdfService.Touch(room, session.PlayerId);

        return Ok(new RoomInfoResponse { Room = room.Name });
    }
}

public record RoomInfoResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "RoomInfo";
    public required string Room { get; init; }
    public string Type { get; init; } = "easy";
    public double? Start { get; init; }
    public double? End { get; init; }
}
