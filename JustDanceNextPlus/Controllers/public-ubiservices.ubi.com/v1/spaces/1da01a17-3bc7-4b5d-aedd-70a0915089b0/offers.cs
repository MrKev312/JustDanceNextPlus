using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/offers")]
public class Offers(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetOffers()
	{
		string offersJson = Path.Combine(pathSettings.Value.JsonsPath, "offers.json");

		if (System.IO.File.Exists(offersJson))
		{
			FileStream stream = System.IO.File.OpenRead(offersJson);
			return File(stream, "application/json");
		}

		return Ok("""
			{
				"offers": []
			}
			""");
	}
}
