using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.songdb.v2;

[ApiController]
[Route("songdb/v2/localisation")]
public class Localisation(IModernJduSongDatabaseService songDatabaseService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetLocalisation()
    {
        return File(songDatabaseService.SerializeLocalisation(), "application/octet-stream");
    }
}
