using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._041c03fa_1735_4ea7_b5fc_c16546d092ca;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("v1/spaces/{spaceId:jdu}/eventsDefinitions")]
public class EventsDefinitions : ControllerBase
{
    [HttpGet]
    public IActionResult GetEventsDefinitions([FromRoute] Guid spaceId)
    {
        return Ok(new
        {
            attributes = Array.Empty<object>(),
            compositions = Array.Empty<object>(),
            signals = Array.Empty<object>()
        });
    }
}
