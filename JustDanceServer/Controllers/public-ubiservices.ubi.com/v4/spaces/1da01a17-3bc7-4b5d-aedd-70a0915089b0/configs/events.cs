using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v4.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.configs;

[ApiController]
[Route("v4/spaces/{spaceId:jdnext}/configs/events")]
public class Events : ControllerBase
{
    [HttpGet]
    public IActionResult GetEvents([FromRoute] Guid spaceId)
    {
        string response = """
			{}
			""";

        return Content(response, "application/json");
    }
}
