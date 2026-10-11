using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.songdb.v1;

[ApiController]
[Route("songdb/v1/songs")]
public sealed class Songs(IClassicJduSongDatabaseService songDatabaseService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSongs([FromHeader(Name = "X-SkuId")] string? skuId)
    {
        if (!ClassicJduClientProfiles.TryGetBySkuId(skuId, out _)
            || !JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? media))
            return BadRequest("X-SkuId does not identify a supported JDU platform.");

        return Ok(songDatabaseService.CreateDatabase(
            media.GameYear >= 2018 ? media.VideoCodec : null,
            media.AssetPlatform));
    }
}
