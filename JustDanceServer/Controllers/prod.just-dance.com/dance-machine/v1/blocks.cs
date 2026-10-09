using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.dance_machine.v1;

[ApiController]
[Route("dance-machine/v1/blocks")]
public sealed class Blocks : ControllerBase
{
    [HttpGet]
    public IActionResult GetBlocks()
        => Ok(new ClassicJduOnlineBlockDatabase());
}
