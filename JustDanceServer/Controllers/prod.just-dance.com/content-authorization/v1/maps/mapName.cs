using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.content_authorization.v1.maps;

[ApiController]
[Route("content-authorization/v1/maps/{mapName}")]
public class MapName(IJduContentAuthorizationService contentAuthorizationService) : ControllerBase
{
    [HttpGet]
    public IActionResult AuthorizeMap(
        [FromRoute] string mapName,
        [FromHeader(Name = "X-SkuId")] string? skuId)
    {
        if (!JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? client))
            return BadRequest("X-SkuId does not identify a supported JDU client.");

        JduContentAuthorizationEntry? authorization = contentAuthorizationService.Create(mapName, client);
        return authorization is null ? NotFound() : Ok(authorization);
    }
}
