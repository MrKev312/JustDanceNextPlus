using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.customizable_itemdb.v1;

[ApiController]
[Route("customizable-itemdb/v1/items")]
public class Items(IJduCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetItems([FromHeader(Name = "X-SkuId")] string? skuId)
    {
		if (catalogService.TryCreateCatalog(JduCatalogFamilies.Items, skuId, out JsonObject catalog))
			return Ok(catalog);

		return Ok(new JsonObject
		{
			["__class"] = "OnlineCustomizableItemDb",
			["avatars"] = new JsonObject(),
			["skins"] = new JsonObject(),
			["portraitBorders"] = new JsonObject()
		});
    }
}
