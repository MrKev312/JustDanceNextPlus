using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/offers")]
public class Offers(IOptions<PathSettings> pathSettings,
    IFileSystem fileSystem) : ControllerBase
{
	[HttpGet]
	public IActionResult GetOffers([FromRoute] Guid spaceId)
	{
		string offersJson = Path.Combine(pathSettings.Value.JsonsPath, "offers.json");

		return fileSystem.FileExists(offersJson)
			? File(fileSystem.OpenRead(offersJson), "application/json")
			: Ok("""
			{
				"offers": []
			}
			""");
	}
}
