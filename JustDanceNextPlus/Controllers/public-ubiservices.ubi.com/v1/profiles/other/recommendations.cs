﻿using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.Utilities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{guid:guid}/recommendations")]
public class Recommendations(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet(Name = "GetRecommendations")]
	public IActionResult GetRecommendations([FromRoute] Guid guid)
	{
		string recommendationsJson = Path.Combine(pathSettings.Value.JsonsPath, "recommendations.json");

		if (System.IO.File.Exists(recommendationsJson))
		{
			FileStream stream = System.IO.File.OpenRead(recommendationsJson);
			return File(stream, "application/json");
		}

		return Ok(new RecommendationsResponse());
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