namespace JustDanceServer.Services;

public sealed record LeaderboardPage(
	IReadOnlyList<LeaderboardPlayerScore> Entries,
	long PlayerCount);

public sealed record LeaderboardPlayerScore(
	Guid ProfileId,
	string Name,
	int CountryId,
	uint AvatarId,
	int Score,
	long Rank,
	int JdPoints,
	uint AliasId,
	int AliasGender,
	uint PortraitBorderId,
	string PlatformId);
