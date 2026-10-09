using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace JustDanceServer.Controllers.prod.just_dance.com.profile.v2;

[ApiController]
[Route("profile/v1/profiles")]
[Route("profile/v2/profiles")]
public class Profiles(
	IClassicProfileService classicProfileService,
	ISessionManager sessionManager,
	IPlayerPreferenceService preferences,
	IPlayerActivityService activity) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetProfiles([FromQuery] string? profileIds)
	{
		Guid[] ids = ParseProfileIds(profileIds);
		IReadOnlyList<JsonObject> profiles = await classicProfileService.GetProfilesAsync(ids);
		if (PlayerRequestContextResolver.FromRequest(Request, sessionManager).SupportsFavoriteMutations)
		{
			foreach (JsonObject profile in profiles)
			{
				if (Guid.TryParse(profile["profileId"]?.ToString(), out Guid id))
					profile["favorites"] = new JsonArray((await preferences.GetFavoritesAsync(id))
						.Select(name => (JsonNode?)JsonValue.Create(name)).ToArray());
			}
		}

		return Ok(profiles);
	}

	[HttpPost]
	public async Task<IActionResult> SaveProfile([FromBody] JsonElement profile)
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();

		bool saved = await classicProfileService.SaveProfileAsync(
			session.PlayerId,
			profile,
			RequestSessionResolver.GetPlatform(Request));
		PlayerRequestContext context = PlayerRequestContextResolver.FromRequest(Request, sessionManager);
		if (saved && context.SupportsFavoriteMutations
			&& profile.TryGetProperty("history", out JsonElement history) && history.ValueKind == JsonValueKind.Object)
		{
			Dictionary<string, long> ordinals = [with(StringComparer.OrdinalIgnoreCase)];
			foreach (JsonProperty map in history.EnumerateObject())
			{
				if (map.Value.ValueKind == JsonValueKind.Number && map.Value.TryGetInt64(out long ordinal) && ordinal > 0)
					ordinals[map.Name] = ordinal;
			}

			await activity.StoreOrdinalsAsync(session.PlayerId, $"jd2017:{session.SessionId:N}", ordinals);
		}

		return saved ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
	}

	private static Guid[] ParseProfileIds(string? profileIds)
		=> string.IsNullOrWhiteSpace(profileIds)
			? []
			: [.. profileIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty)
				.Where(id => id != Guid.Empty)];
}
