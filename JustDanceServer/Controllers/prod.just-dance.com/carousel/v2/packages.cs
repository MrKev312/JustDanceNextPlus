using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.carousel.v2;

[ApiController]
[Route("carousel/v2/packages")]
public class Packages : ControllerBase
{
    [HttpPost]
    public IActionResult GetPackages()
    {
        return Ok(new PackageIdsResponse());
    }
}

public record PackageIdsResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "PackageIds";
    public string[] PackageIds { get; init; } = [];
}
