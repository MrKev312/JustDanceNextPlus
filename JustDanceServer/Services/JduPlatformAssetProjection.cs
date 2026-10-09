namespace JustDanceServer.Services;

internal static class JduPlatformAssetProjection
{
	private static readonly IReadOnlyDictionary<string, string> FieldsByRole =
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["BannerBackground"] = "banner_bkgImageUrl",
			["AlbumBackground"] = "expandBkgImageUrl",
			["MapBackground"] = "map_bkgImageUrl",
			["AlbumCoach"] = "expandCoachImageUrl",
			["CoverSmall"] = "cover_smallImageUrl",
			["Cover"] = "coverImageUrl"
		};

	public static IReadOnlyDictionary<string, string> Create(
		IReadOnlyDictionary<string, string> assetsByRole,
		int coachCount,
		string mapName,
		string platform)
	{
		Dictionary<string, string> fields = [with(StringComparer.Ordinal)];
		foreach ((string role, string field) in FieldsByRole)
			fields[field] = RequireAsset(assetsByRole, role, mapName, platform);

		for (int coach = 1; coach <= coachCount; coach++)
		{
			fields[$"coach{coach}ImageUrl"] = RequireAsset(
				assetsByRole,
				$"Coach{coach}",
				mapName,
				platform);
		}

		return fields;
	}

	private static string RequireAsset(
		IReadOnlyDictionary<string, string> assetsByRole,
		string role,
		string mapName,
		string platform)
		=> assetsByRole.TryGetValue(role, out string? url)
			? url
			: throw new InvalidDataException(
				$"Map '{mapName}' is missing required {role} assets for {platform}.");
}
