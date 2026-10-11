using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.carousel.v2.pages;

[ApiController]
[Route("carousel/v2/pages")]
public sealed class StaticCatalogs(IJduCatalogService catalogService) : ControllerBase
{
    [HttpPost("avatars")]
    public IActionResult GetAvatars([FromHeader(Name = "X-SkuId")] string? skuId)
        => GetCatalog(JduCatalogFamilies.CarouselAvatars, skuId);

    [HttpPost("dancerprofile")]
    public IActionResult GetDancerProfile([FromHeader(Name = "X-SkuId")] string? skuId)
        => GetCatalog(JduCatalogFamilies.CarouselDancerProfile, skuId);

    [HttpPost("quests")]
    public IActionResult GetQuests([FromHeader(Name = "X-SkuId")] string? skuId)
        => GetCatalog(JduCatalogFamilies.CarouselQuests, skuId);

    private IActionResult GetCatalog(string family, string? skuId)
        => catalogService.TryCreateCatalog(family, skuId, out JsonObject catalog)
            ? Ok(catalog)
            : Ok(new JsonObject
            {
                ["__class"] = "JD_CarouselContent",
                ["categories"] = new JsonArray()
            });
}
