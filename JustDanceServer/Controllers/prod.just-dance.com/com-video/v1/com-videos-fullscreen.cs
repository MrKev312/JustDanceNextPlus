using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.com_video.v1;

[ApiController]
[Route("com-video/v1/com-videos-fullscreen")]
public sealed class ComVideosFullscreen : ControllerBase
{
    [HttpGet]
    public IActionResult GetVideos()
    {
        return Ok(Array.Empty<object>());
    }
}
