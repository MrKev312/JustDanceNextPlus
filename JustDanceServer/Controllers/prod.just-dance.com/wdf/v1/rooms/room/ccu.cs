using Microsoft.AspNetCore.Mvc;
using JustDanceServer.Services;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/ccu")]
public class Ccu(IWdfService wdfService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCcu([FromRoute] string room)
    {
		if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
			return BadRequest();

        return Content(wdfService.GetActivePlayerCount(resolvedRoom).ToString(), "text/html");
    }
}
