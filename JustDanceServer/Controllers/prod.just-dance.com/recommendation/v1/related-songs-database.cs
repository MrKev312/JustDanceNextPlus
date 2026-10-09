using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceServer.Controllers.prod.just_dance.com.recommendation.v1;

[ApiController]
[Route("recommendation/v1/related-songs/database")]
public class RelatedSongsDatabase(IJduRecommendationService recommendationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDatabase()
    {
        IReadOnlyDictionary<string, string[]> database =
			await recommendationService.GetRelatedSongsAsync();
		return File(JsonSerializer.SerializeToUtf8Bytes(database), "application/octet-stream");
    }
}
