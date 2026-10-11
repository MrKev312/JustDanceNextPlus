using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.status.v1;

[ApiController]
[Route("status/v1/ping")]
public class Ping : ControllerBase
{
    [HttpGet]
    public IActionResult GetPing()
    {
        return Ok();
    }
}
