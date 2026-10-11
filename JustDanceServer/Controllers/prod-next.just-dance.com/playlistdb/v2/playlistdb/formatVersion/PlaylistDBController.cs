using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.playlistdb.v2.playlistdb.formatVersion;

[ApiController]
[Route("playlistdb/v2/playlistdb/formatVersion/v0")]
public class PlaylistDBController(IPlaylistService playlistService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetFormatVersion()
    {
        string platform = ClientPlatform.FromRequest(Request);
        return Ok(playlistService.GetPlaylistDB(platform));
    }
}
