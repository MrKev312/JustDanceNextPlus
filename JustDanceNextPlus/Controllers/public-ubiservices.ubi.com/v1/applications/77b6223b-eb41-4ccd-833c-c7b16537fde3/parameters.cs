using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.applications._77b6223b_eb41_4ccd_833c_c7b16537fde3;

[ApiController]
[Route("/v1/applications/77b6223b-eb41-4ccd-833c-c7b16537fde3/parameters")]
public class Parameters(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet(Name = "GetParameters")]
	public IActionResult GetParameters()
	{
		string parametersJson = Path.Combine(pathSettings.Value.JsonsPath, "parameters.json");

		return Ok(System.IO.File.ReadAllText(parametersJson));
	}
}
