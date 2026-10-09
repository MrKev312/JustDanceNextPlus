using JustDanceServer.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.recommendation.v1;

[ApiController]
[Route("recommendation/v1/related-songs")]
public class RelatedSongs(IOptions<UrlSettings> urlSettings) : ControllerBase
{
    [HttpGet]
    public IActionResult GetRelatedSongs()
    {
        string host = string.IsNullOrWhiteSpace(urlSettings.Value.JduHostUrl)
            ? urlSettings.Value.HostUrl
            : urlSettings.Value.JduHostUrl;

        return Ok(new RelatedSongsResponse
        {
            RelatedSongs = $"https://{host}/recommendation/v1/related-songs/database"
        });
    }
}

public record RelatedSongsResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "RelatedSongs";
    public string RelatedSongs { get; init; } = string.Empty;
}
