using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v2;

[ApiController]
[Route("v2/websocket")]
public class Websocket : ControllerBase
{
	[HttpGet]
	public IActionResult GetWebsocket()
	{
		// Stub: phone app checks for websocket availability
		return Ok(new { });
	}
}
