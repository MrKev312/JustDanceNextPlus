using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.session.v2;

[ApiController]
[Route("session/v2/pairing-info")]
public class PairingInfo(IPairingService pairingService, IOptions<UrlSettings> urlSettings) : ControllerBase
{
    [HttpGet(Name = "GetPairingInfo")]
    public IActionResult GetPairingInfo([FromQuery] string code)
    {
        PairingEntry? entry = pairingService.GetPairingInfo(code);

        if (entry is null)
            return NotFound(new { error = "Pairing code not found" });

        return Ok(PairingCodeInfoResponse.FromEntry(entry, urlSettings.Value.HostUrl));
    }
}
