using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.battlepasses;

[ApiController]
[Route("v2/spaces/{spaceId:jdnext}/battlepasses/seasons")]
public class Seasons : ControllerBase
{
    [HttpGet]
    public IActionResult GetSeasons([FromRoute] Guid spaceId)
    {
        string response = """
			{
				"seasons": []
			}
			""";
        return Content(response, "application/json");
    }
}
