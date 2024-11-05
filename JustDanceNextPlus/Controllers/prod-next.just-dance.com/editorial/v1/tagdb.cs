using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/tagdb")]
public class TagDB(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetTagdb()
	{
		string response = System.IO.File.ReadAllText(Path.Combine(pathSettings.Value.JsonsPath, "tagdb.json"));

		return Content(response, "application/json");
	}
}
