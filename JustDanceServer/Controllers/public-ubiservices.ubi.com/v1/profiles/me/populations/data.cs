using Microsoft.AspNetCore.Mvc;

using JustDanceServer.Configuration;

using System.Text.Json;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.profiles.me.populations;

[ApiController]
[Route("v1/profiles/me/populations/data")]
public class Data : ControllerBase
{
	[HttpPut]
	public IActionResult PutPopulationsData([FromBody] JsonElement body)
	{
		if (!body.TryGetProperty("spaceId", out JsonElement spaceIdElement)
			|| !Guid.TryParse(spaceIdElement.GetString(), out Guid spaceId))
			return BadRequest("Missing or invalid 'spaceId' field in request body");

		if (ModernJduClientProfiles.TryGetBySpaceId(spaceId, out ModernJduClientProfile? client)
			&& client.TargetSpaceId.HasValue)
			return Ok(new { populations = Array.Empty<object>() });

		if (spaceId == Jdu2022ClientProfiles.Game.TargetSpaceId)
			return Ok(new { populations = CreateJdu2022Populations() });

		if (spaceId == Jdu2021ClientProfiles.Game.TargetSpaceId)
			return Ok(new { populations = new[] { CreateJdu2019Population(spaceId) } });

		if (spaceId == JdNextConstants.SessionSpaceId)
		{
			return Ok(new
			{
				populations = CreatePopulations(spaceId,
					("prod", "us_staging", "2024-10-01T09:29:46Z"),
					("NoRemoteLog", "remotelogs", "2024-10-03T20:06:27Z"))
			});
		}

		if (spaceId == JdNextConstants.GameSpaceId)
		{
			return Ok(new
			{
				populations = CreatePopulations(spaceId,
					("Switch", "Platform", "2024-06-14T13:32:40Z"),
					("CTR", "ABTestCategoriesAlgorithm", "2024-05-30T13:29:32Z"),
					("ALSComplexTuned", "ABTestCategoriesALS", "2024-05-30T13:29:32Z"),
					("DetailedDancemoveTrackingDisabled", "DetailedDancemoveTracking", "2024-10-03T20:06:27Z"),
					("DetailedQualityStreamTrackingDisabled", "DetailedQualityStreamTracking", "2024-10-03T20:06:27Z"),
					("Public", "Endpoint", "2024-05-30T13:29:32Z"),
					("Errors", "RemoteLogs", "2024-10-03T20:06:27Z"),
					("SkeletonTrackingDisabled", "SkeletonTracking", "2024-10-03T20:06:27Z"),
					("NoTests", "SSLPinningCanary", "2024-10-03T20:06:27Z"))
			});
		}

		return BadRequest("Invalid 'spaceId' field in request body");
	}

	private static object[] CreatePopulations(
		Guid spaceId,
		params (string Name, string Subject, string AssignmentTime)[] values)
	{
		return
		[
			.. values.Select(value => (object)new
			{
				name = value.Name,
				obj = new { },
				subject = value.Subject,
				spaceId,
				assignmentTime = value.AssignmentTime
			})
		];
	}

	internal static object[] CreateJdu2022Populations()
	{
		(string Name, string Subject, object Value)[] values =
		[
			("DEFAULT_COVER", "ABTestPlaylistCovers", new { }),
			("LengthOf10", "ABTestPlaylistLengthForPlaylistTab", new { playlistLength = 10 }),
			("DiscoveryJDU", "ABTestQuickplayDesignRules", new { }),
			("ALS", "ABTestQuickplayRecoModel", new { }),
			("ALS", "ABTestRelevanceRecoModel", new { }),
			("Disabled", "ABTestRSSAutoplay", new { }),
			("B", "ABTestRSSAutoplayTimer", new { }),
			("Recommendation", "ABTestSongsSortingTypeInSongsTabCategories", new { }),
			("ALS", "ABTestSortingRecoModel", new { }),
			("PROD", "Environment", new { }),
			("PlaylistTracksOrderPattern_Blocks_SKUFirst", "PlaylistTracksOrderPattern", new { }),
			("Off", "QCTesting", new { }),
			("NOT_TRACKED", "TRACK_MOVES", new { })
		];

		string assignmentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
		return
		[
			.. values.Select(value => (object)new
			{
				name = value.Name,
				obj = value.Value,
				subject = value.Subject,
				spaceId = Jdu2022ClientProfiles.Game.TargetSpaceId!.Value,
				assignmentTime
			})
		];
	}

	internal static object CreateJdu2019Population(Guid spaceId)
	{
		return new
		{
			name = "WithoutLogs",
			obj = new { },
			subject = "RemoteLogs",
			spaceId,
			assignmentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
		};
	}
}
