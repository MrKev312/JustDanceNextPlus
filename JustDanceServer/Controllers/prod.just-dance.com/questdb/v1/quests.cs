using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.questdb.v1;
[ApiController]
[Route("questdb/v1/quests")]
public sealed class Quests(IJduCatalogService catalogService) : ControllerBase
{
    [HttpGet]
	public IActionResult GetQuests([FromHeader(Name = "X-SkuId")] string? skuId)
		=> catalogService.TryCreateCatalog(JduCatalogFamilies.Quests, skuId, out JsonObject catalog)
			? Ok(catalog)
			: Ok(new JsonObject
			{
				["__class"] = "OnlineQuestDb",
				["quests"] = new JsonArray()
			});
}
