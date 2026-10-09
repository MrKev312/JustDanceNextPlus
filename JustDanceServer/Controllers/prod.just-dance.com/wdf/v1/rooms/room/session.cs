using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1.rooms.room;

[ApiController]
[Route("wdf/v1/rooms/{room}/session")]
public sealed class WdfSession(IWdfService wdfService, ISessionManager sessionManager) : ControllerBase
{
	[HttpGet]
	[HttpPost]
	public IActionResult Join([FromRoute] string room)
	{
		if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
			return BadRequest();

		if (RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			wdfService.BeginSession(resolvedRoom, session.PlayerId);

		return Content("OK", "text/plain");
	}

	[HttpDelete]
	public IActionResult Leave([FromRoute] string room)
	{
		if (!WdfRequestResolver.TryResolveRoom(Request, room, wdfService, out WdfRoomId resolvedRoom))
			return BadRequest();

		if (RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			wdfService.Leave(resolvedRoom, session.PlayerId);

		return Ok();
	}
}
