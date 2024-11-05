using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1.activity_page.formatVersion;

[ApiController]
[Route("editorial/v1/activity-page/formatVersion/v0")]
public class V0(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetActivityPage()
	{
		return Ok(System.IO.File.ReadAllText(Path.Combine(pathSettings.Value.JsonsPath, "activity-page.json")));
	}
}
