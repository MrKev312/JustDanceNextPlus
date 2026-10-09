using HotChocolate;

using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.GraphQL.Objects;
using JustDanceServer.Services;

namespace JustDanceServer.JustDanceClasses.GraphQL;

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
		Guid[] parsedIds = ParseIds(ids);
		IReadOnlyDictionary<Guid, Profile> profiles = await userDataService.GetProfilesByIdsAsync(parsedIds);
		return [.. parsedIds.Where(profiles.ContainsKey).Select(id => GraphQlProfile.FromProfile(profiles[id]))];
	}

	[GraphQLName("getProfilesLeaderboardById")]
	public async Task<List<GraphQlLeaderboardProfile>> GetProfilesLeaderboardById(
		List<string> ids,
		string? mapId,
		string? playlistId,
		[Service] IUserDataService userDataService,
		[Service] IMapService mapService)
	{
		Guid mapGuid = Guid.TryParse(mapId, out Guid parsedMapId) ? parsedMapId : Guid.Empty;
		Guid playlistGuid = Guid.TryParse(playlistId, out Guid parsedPlaylistId) ? parsedPlaylistId : Guid.Empty;
		string? mapName = mapService.TryGetMapName(mapGuid, out string resolvedMapName)
			? resolvedMapName
			: null;

		Guid[] parsedIds = ParseIds(ids);
		IReadOnlyDictionary<Guid, Profile> profiles = await userDataService.GetProfilesByIdsAsync(parsedIds);
		return [.. parsedIds.Where(profiles.ContainsKey)
			.Select(id => GraphQlLeaderboardProfile.FromProfile(profiles[id], mapName, playlistGuid))];
	}

	private static Guid[] ParseIds(IEnumerable<string> ids)
		=> [.. ids.Select(id => Guid.TryParse(id, out Guid parsed) ? (Guid?)parsed : null)
			.Where(id => id.HasValue).Select(id => id!.Value)];
}
