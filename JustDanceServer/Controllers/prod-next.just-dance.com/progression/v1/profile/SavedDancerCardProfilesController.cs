using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile;

// JD2027 uses a JSON POST. Keep the legacy query-string GET controller unchanged.
[ApiController]
[Route("progression/v1/profile/profiles")]
public sealed class SavedDancerCardProfilesController(ISavedDancerCardService cards) : ControllerBase
{
	[HttpPost]
	public async Task<IActionResult> Post([FromBody] ProfilesBody request)
	{
		var found = await cards.GetPublicAsync(request.Pids);
		List<object> profiles = [];
		foreach (Guid id in request.Pids)
		{
			// Captured responses keep unknown IDs as id-only entries, in request order.
			if (found.TryGetValue(id, out var card))
				profiles.Add(new { id, dancercard = card.ToPublic() });
			else
				profiles.Add(new { id });
		}

		return Ok(new { profiles });
	}

	public sealed class ProfilesBody
	{
		[Required, MaxLength(500)]
		public List<Guid> Pids { get; init; } = [];
	}
}
