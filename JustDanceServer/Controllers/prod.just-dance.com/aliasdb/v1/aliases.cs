using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Services;

using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.aliasdb.v1;

[ApiController]
[Route("aliasdb/v1/aliases")]
public class Aliases(IJduCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAliases([FromHeader(Name = "X-SkuId")] string? skuId)
    {
        return catalogService.TryCreateCatalog(JduCatalogFamilies.Aliases, skuId, out JsonObject catalog)
            ? Ok(catalog)
            : Ok(new JsonObject
            {
                ["__class"] = "OnlineAliasDb",
                ["aliases"] = new JsonObject()
            });
    }
}
