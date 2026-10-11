using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.content_authorization.v1;

[ApiController]
[Route("content-authorization/v1/blocks")]
public class Blocks : ControllerBase
{
    [HttpPost]
    public IActionResult AuthorizeBlocks()
    {
        return Ok(new { entries = Array.Empty<object>() });
    }
}
