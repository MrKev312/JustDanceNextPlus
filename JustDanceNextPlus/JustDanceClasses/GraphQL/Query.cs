using HotChocolate;

using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;
using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL;

public class Query
{
	[GraphQLName("getProfileById")]
	public async Task<GraphQlProfile?> GetProfileById(string id, [Service] IUserDataService userDataService)
	{
		if (!Guid.TryParse(id, out Guid guid))
			return null;

		Profile? profile = await userDataService.GetProfileByIdAsync(guid);
		return profile == null ? null : GraphQlProfile.FromProfile(profile);
	}

	[GraphQLName("getProfilesById")]
	public async Task<List<GraphQlProfile>> GetProfilesById(List<string> ids, [Service] IUserDataService userDataService)
	{
		List<GraphQlProfile> profiles = [];

		foreach (string id in ids)
		{
			if (!Guid.TryParse(id, out Guid guid))
				continue;

			Profile? profile = await userDataService.GetProfileByIdAsync(guid);
			if (profile != null)
				profiles.Add(GraphQlProfile.FromProfile(profile));
		}

		return profiles;
	}

	[GraphQLName("getProfilesLeaderboardById")]
	public async Task<List<GraphQlLeaderboardProfile>> GetProfilesLeaderboardById(
		List<string> ids,
		string? mapId,
		string? playlistId,
		[Service] IUserDataService userDataService)
	{
		Guid.TryParse(mapId, out Guid mapGuid);
		Guid.TryParse(playlistId, out Guid playlistGuid);

		List<GraphQlLeaderboardProfile> profiles = [];

		foreach (string id in ids)
		{
			if (!Guid.TryParse(id, out Guid profileGuid))
				continue;

			Profile? profile = await userDataService.GetProfileByIdAsync(profileGuid);
			if (profile != null)
				profiles.Add(GraphQlLeaderboardProfile.FromProfile(profile, mapGuid, playlistGuid));
		}

		return profiles;
	}
}
