using HotChocolate;

using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL;

public class Query
{
	[GraphQLName("getProfileById")]
	[GraphQLDescription("Get a profile by its id")]
	public async Task<Profile?> GetProfileById(string id, [Service] UserDataService userDataService)
	{
		// Parse id to Guid
		if (!Guid.TryParse(id, out Guid guid))
			return null;

		return await userDataService.GetProfileByIdAsync(guid);
	}

	[GraphQLDescription("Check if a profile exists")]
	public async Task<Profile?> HasJustDanceProfile(string id, [Service] UserDataService userDataService)
	{
		// Parse id to Guid
		if (!Guid.TryParse(id, out Guid guid))
			return null;

		return await userDataService.GetProfileByIdAsync(guid);
	}

	[GraphQLName("getProfilesLeaderboardById")]
	[GraphQLDescription("Get profiles leaderboard by their ids")]
	public async Task<List<Profile>> GetProfilesLeaderboardById(List<string> ids, string mapId, [Service] UserDataService userDataService)
	{
		List<Profile> profiles = [];

		foreach (string id in ids)
		{
			if (Guid.TryParse(id, out Guid guid))
			{
				Profile? profile = await userDataService.GetProfileByIdAsync(guid);

				if (profile != null)
					profiles.Add(profile);
			}
		}

		return profiles;
	}
}

