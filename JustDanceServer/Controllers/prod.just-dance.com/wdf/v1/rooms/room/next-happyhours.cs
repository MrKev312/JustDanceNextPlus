using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/next-happyhours")]
public class NextHappyHours(IWdfService wdfService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetNextHappyHours([FromRoute] string room)
    {
        if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out _))
            return BadRequest();

        return Ok(wdfService.GetHappyHours());
    }
}
