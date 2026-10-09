using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.avatardb.v1;

[ApiController]
[Route("avatardb/v1/avatars")]
public sealed class Avatars(IJduCatalogService catalogService) : ControllerBase
{
    [HttpGet]
	public IActionResult GetAvatars([FromHeader(Name = "X-SkuId")] string? skuId)
		=> catalogService.TryCreateLegacyAvatarCatalog(skuId, out JsonObject catalog)
			? Ok(catalog)
			: Ok(new JsonObject
			{
				["__class"] = "OnlineAvatarDb",
				["avatars"] = new JsonObject()
			});
}
