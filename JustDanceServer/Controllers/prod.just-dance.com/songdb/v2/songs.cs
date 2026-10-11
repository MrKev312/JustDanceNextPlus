using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.prod.just_dance.com.songdb.v2;

[ApiController]
[Route("songdb/v2/songs")]
public class Songs(IOptions<UrlSettings> urlSettings) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSongs([FromHeader(Name = "X-SkuId")] string? skuId)
    {
        string? assetPlatform = JduAssetPlatform.FromSkuId(skuId);
        if (assetPlatform is null)
            return BadRequest("X-SkuId does not identify a supported JDU platform.");

        string host = string.IsNullOrWhiteSpace(urlSettings.Value.JduHostUrl)
            ? urlSettings.Value.HostUrl
            : urlSettings.Value.JduHostUrl;

        return Ok(new SongDbResponse
        {
            SongdbUrl = $"https://{host}/songdb/v2/database?assetPlatform={Uri.EscapeDataString(assetPlatform)}",
            LocalisationUrl = $"https://{host}/songdb/v2/localisation"
        });
    }
}

public record SongDbResponse
{
    public Dictionary<string, object> RequestSpecificMaps { get; init; } = [];
    public string SongdbUrl { get; init; } = string.Empty;
    public string LocalisationUrl { get; init; } = string.Empty;
    public string[] LocalTracks { get; init; } = [];
}
