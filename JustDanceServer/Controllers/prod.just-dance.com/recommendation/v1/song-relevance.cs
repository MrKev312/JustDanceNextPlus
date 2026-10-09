using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.recommendation.v1;

[ApiController]
[Route("recommendation/v1/song-relevance")]
public class SongRelevance(IJduRecommendationService recommendationService, ISessionManager sessions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSongRelevance()
    {
        return Ok(new RecommendedSongsResponse
		{
			OrderedMaps = await recommendationService.GetOrderedMapsAsync(PlayerRequestContextResolver.FromRequest(Request, sessions).ProfileId)
		});
    }
}

public record RecommendedSongsResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "RecommendedSongs";
    public string[] OrderedMaps { get; init; } = [];
}
