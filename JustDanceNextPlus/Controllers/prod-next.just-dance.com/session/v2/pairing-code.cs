using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.session.v2;

[ApiController]
[Route("session/v2/pairing-code")]
public class PairingCode : ControllerBase
{
	[HttpPost(Name = "PostPairingCode")]
	public IActionResult PostPairingCode([FromBody] JsonElement body)
	{
		string response = $$"""
		{
			"pairingCode": "scanme:3",
			"qrCodePairingUrl": "https://youtu.be/dQw4w9WgXcQ"
		}
		""";

		return Content(response, "application/json");
	}
}
