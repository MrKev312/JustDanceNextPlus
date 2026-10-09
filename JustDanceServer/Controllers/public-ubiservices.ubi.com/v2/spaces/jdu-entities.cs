using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces;

[ApiController]
[Route("v2/spaces")]
public sealed class JduEntities(
    IClassicJduDiscoveryService classicDiscoveryService,
    IModernJduDiscoveryService modernDiscoveryService) : ControllerBase
{
    [HttpGet("{spaceId:guid}/entities")]
    public IActionResult GetEntities(Guid spaceId)
    {
        if (ClassicJduClientProfiles.TryGetBySpaceId(
            spaceId,
            out ClassicJduClientProfile? classicProfile))
        {
            return Ok(new { entities = new[] { classicDiscoveryService.CreateEntity(classicProfile) } });
        }

        if (ModernJduClientProfiles.IsKnownSpaceId(spaceId))
        {
            return Ok(new { entities = new[] { modernDiscoveryService.CreateServerEntity(spaceId) } });
        }

        return NotFound();
    }
}
