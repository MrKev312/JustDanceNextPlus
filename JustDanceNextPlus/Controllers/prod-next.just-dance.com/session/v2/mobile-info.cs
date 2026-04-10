using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.session.v2;

[ApiController]
[Route("session/v2/mobile-info")]
public class MobileInfo(IPairingService pairingService, IOptions<UrlSettings> urlSettings) : ControllerBase
{
	[HttpPost(Name = "PostMobileInfo")]
	public IActionResult PostMobileInfo([FromQuery] string code, [FromBody] JsonElement body)
	{
		PairingEntry? existing = pairingService.GetPairingInfo(code);
		if (existing is null)
			return NotFound(new { error = "Pairing code not found" });

		string language = body.TryGetProperty("language", out JsonElement langElem)
			? langElem.GetString() ?? "en"
			: "en";

		string deviceId = body.TryGetProperty("deviceId", out JsonElement devElem)
			? devElem.GetString() ?? Guid.NewGuid().ToString()
			: Guid.NewGuid().ToString();

		string pkcs12Certificate = body.TryGetProperty("pkcs12Certificate", out JsonElement certElem)
			? certElem.GetString() ?? ""
			: "";

		int platform = body.TryGetProperty("platform", out JsonElement platElem)
			? platElem.GetInt32()
			: 11; // Android

		PairedPhone phone = new()
		{
			Language = language,
			DeviceId = deviceId,
			Pkcs12Certificate = pkcs12Certificate,
			Platform = platform
		};

		PairingEntry? updated = pairingService.AddPhone(code, phone);
		if (updated is null)
			return NotFound(new { error = "Pairing code not found" });

		return Ok(PairingCodeInfoResponse.FromEntry(updated, urlSettings.Value.HostUrl));
	}

	[HttpDelete(Name = "DeleteMobileInfo")]
	public IActionResult DeleteMobileInfo([FromQuery] string code)
	{
		string? deviceId = null;

		// Try to get deviceId from query or session context
		if (Request.Query.TryGetValue("deviceId", out var devIdValues))
			deviceId = devIdValues.ToString();

		if (string.IsNullOrEmpty(deviceId))
		{
			// Fall back to ubi-sessionid as device identifier
			deviceId = Request.Headers["ubi-sessionid"].ToString();
		}

		if (string.IsNullOrEmpty(deviceId))
			return BadRequest(new { error = "Cannot determine device identity" });

		bool removed = pairingService.RemovePhone(code, deviceId);
		if (!removed)
			return NotFound(new { error = "Pairing code or device not found" });

		return Ok(new { pairingCode = code });
	}
}
