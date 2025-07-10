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
		string filePath = Path.Combine(pathSettings.Value.JsonsPath, "activity-page.json");

		if (!System.IO.File.Exists(filePath))
			return NotFound();

		FileStream stream = System.IO.File.OpenRead(filePath);
		return File(stream, "application/json");
	}
}
