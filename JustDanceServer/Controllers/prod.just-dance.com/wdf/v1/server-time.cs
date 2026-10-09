using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1;

[ApiController]
[Route("wdf/v1/server-time")]
public class ServerTime(TimeProvider timeProvider) : ControllerBase
{
    [HttpGet]
    public IActionResult GetServerTime()
    {
        return Ok(new { time = timeProvider.GetUtcNow().ToUnixTimeMilliseconds() / 1000d });
    }
}
