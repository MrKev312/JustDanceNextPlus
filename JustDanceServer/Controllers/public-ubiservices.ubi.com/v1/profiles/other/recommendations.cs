using JustDanceServer.Configuration;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{guid:guid}/recommendations")]
public class Recommendations(IOptions<PathSettings> pathSettings,
    IFileSystem fileSystem) : ControllerBase
{
	[HttpGet(Name = "GetRecommendations")]
	public IActionResult GetRecommendations([FromRoute] Guid guid)
	{
		string recommendationsJson = Path.Combine(pathSettings.Value.JsonsPath, "recommendations.json");

		return fileSystem.FileExists(recommendationsJson)
			? File(fileSystem.OpenRead(recommendationsJson), "application/json")
			: Ok(new RecommendationsResponse());
	}
}

public class RecommendationsResponse
{
	public Dictionary<string, List<Recommendation>> Recommendations { get; set; } = [];
}

public class Recommendation
{
	public Guid Id { get; set; }
	[JsonDateFormat("yyyy-MM-dd HH:mm:ss")]
	public DateTime ModifiedAt { get; set; }
}