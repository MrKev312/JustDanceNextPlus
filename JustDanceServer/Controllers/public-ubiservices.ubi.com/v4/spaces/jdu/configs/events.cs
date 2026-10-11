using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v4.spaces.jdu.configs;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("v4/spaces/{spaceId:jdu}/configs/events")]
public sealed class Events : ControllerBase
{
    [HttpGet]
    public IActionResult GetEvents(Guid spaceId)
    {
        return Ok(new
        {
            types = Array.Empty<string>(),
            s2sConfig = new { },
            publicConfig = new { }
        });
    }
}
