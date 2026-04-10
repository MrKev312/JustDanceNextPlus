using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.session.v2;

[ApiController]
[Route("session/v2/pairing-code")]
public class PairingCode(IPairingService pairingService, IOptions<UrlSettings> urlSettings) : ControllerBase
{
	[HttpPost(Name = "PostPairingCode")]
	public IActionResult PostPairingCode([FromBody] JsonElement body)
	{
		string ipAddress = body.TryGetProperty("deviceIP", out JsonElement ipElem)
			? ipElem.GetString() ?? ""
			: "";

		int port = body.TryGetProperty("devicePort", out JsonElement portElem)
			? portElem.GetInt32()
			: 0;

		string pkcs12Certificate = body.TryGetProperty("pkcs12Certificate", out JsonElement certElem)
			? certElem.GetString() ?? ""
			: "";

		string tlsCertificate = body.TryGetProperty("tlsCertificate", out JsonElement tlsCertElem)
			? tlsCertElem.GetString() ?? ""
			: "";

		string protocol = body.TryGetProperty("protocol", out JsonElement protoElem)
			? protoElem.GetString() ?? "keel"
			: "keel";

		string titleId = body.TryGetProperty("titleId", out JsonElement titleElem)
			? titleElem.GetString() ?? ""
			: "";

		string displayName = body.TryGetProperty("displayName", out JsonElement nameElem)
			? nameElem.GetString() ?? "JustDanceNextPlus"
			: "JustDanceNextPlus";

		int platform = body.TryGetProperty("platform", out JsonElement platElem)
			? platElem.GetInt32()
			: 38; // Switch

		PairingEntry entry = pairingService.CreatePairing(ipAddress, port, pkcs12Certificate, tlsCertificate, protocol, titleId, displayName, platform);

		PairingCodeResponse response = new()
		{
			PairingCode = entry.PairingCode,
			QrCodePairingUrl = $"https://jd-s3.cdn.ubi.com/sca/redirect.html?pc={entry.PairingCode}"
		};

		return Ok(response);
	}
}
