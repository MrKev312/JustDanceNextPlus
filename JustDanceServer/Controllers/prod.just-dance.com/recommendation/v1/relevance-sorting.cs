using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.recommendation.v1;

[ApiController]
[Route("recommendation/v1/relevance-sorting")]
public class RelevanceSorting(IJduRecommendationService recommendationService, ISessionManager sessions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRelevanceSorting()
    {
        return Ok(new RecommendedSongsResponse
        {
            OrderedMaps = await recommendationService.GetOrderedMapsAsync(PlayerRequestContextResolver.FromRequest(Request, sessions).ProfileId)
        });
    }
}
