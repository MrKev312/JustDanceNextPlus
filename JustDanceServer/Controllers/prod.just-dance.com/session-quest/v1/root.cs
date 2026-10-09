using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.session_quest.v1;

[ApiController]
[Route("session-quest/v1/")]
public class Root(IJduCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetSessionQuest([FromHeader(Name = "X-SkuId")] string? skuId)
    {
		return catalogService.TryCreateCatalog(JduCatalogFamilies.SessionQuests, skuId, out JsonObject catalog)
			? Ok(catalog)
			: Ok(new JsonObject
			{
				["__class"] = "SessionQuestService::QuestData",
				["newReleases"] = new JsonArray()
			});
    }
}
