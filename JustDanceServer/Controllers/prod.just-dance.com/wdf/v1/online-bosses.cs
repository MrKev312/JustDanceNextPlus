using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceServer.Controllers.prod.just_dance.com.wdf.v1;

[ApiController]
[Route("wdf/v1/online-bosses")]
public class OnlineBosses : ControllerBase
{
    [HttpGet]
    public IActionResult GetOnlineBosses()
    {
        return Ok(new OnlineBossDbResponse());
    }
}

public record OnlineBossDbResponse
{
    [JsonPropertyName("__class")]
    public string Class { get; init; } = "OnlineBossDb";
    public Dictionary<string, object> Bosses { get; init; } = [];
}
