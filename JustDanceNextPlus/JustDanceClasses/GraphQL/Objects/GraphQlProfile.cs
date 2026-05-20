using JustDanceNextPlus.JustDanceClasses.Database.Profile;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;

public class GraphQlProfile
{
	public required Guid Id { get; init; }
	public required DancerCard Dancercard { get; init; }

	public static GraphQlProfile FromProfile(Profile profile)
	{
		return new GraphQlProfile
		{
			Id = profile.Id,
			Dancercard = profile.Dancercard
		};
	}
}

public class GraphQlLeaderboardProfile : GraphQlProfile
{
	public GraphQlPlatformStat MapStat { get; init; } = new();
	public GraphQlPlatformStat PlaylistStat { get; init; } = new();
	public Ownership Ownership { get; init; } = new();

	public static GraphQlLeaderboardProfile FromProfile(Profile profile, Guid mapId, Guid playlistId)
	{
		return new GraphQlLeaderboardProfile
		{
			Id = profile.Id,
			Dancercard = profile.Dancercard,
			MapStat = new GraphQlPlatformStat
			{
				Platform = mapId != Guid.Empty && profile.MapStats.TryGetValue(mapId, out MapStats? mapStats)
					? mapStats.Platform
					: "uplay"
			},
			PlaylistStat = new GraphQlPlatformStat
			{
				Platform = playlistId != Guid.Empty && profile.PlaylistStats.TryGetValue(playlistId, out PlaylistStats? playlistStats)
					? playlistStats.Platform
					: "uplay"
			},
			Ownership = profile.Ownership
		};
	}
}

public class GraphQlPlatformStat
{
	public string Platform { get; set; } = "uplay";
}
