using JustDanceServer.Services;

using System.Diagnostics.CodeAnalysis;

namespace JustDanceServer.Configuration;

public sealed record JduMediaClientProfile(
	int GameYear,
	JduPlatform Platform,
	string AssetPlatform,
	MapVideoCodec VideoCodec);

public static class JduVideoCapabilities
{
	private static readonly JduPlatform[] Vp9Platforms =
	[
		JduPlatform.NintendoSwitch
	];
	private static readonly JduPlatform[] PlatformsSupporting1080p =
	[
		JduPlatform.Pc,
		JduPlatform.NintendoSwitch,
		JduPlatform.PlayStation4,
		JduPlatform.PlayStation5,
		JduPlatform.XboxOne
	];

	public static MapVideoCodec GetCodec(int gameYear, JduPlatform platform)
		=> gameYear >= 2018 && Vp9Platforms.Contains(platform)
			? MapVideoCodec.Vp9
			: MapVideoCodec.Vp8;

	public static bool Supports1080p(JduPlatform platform)
		=> PlatformsSupporting1080p.Contains(platform);
}

public static class JduMediaClientProfiles
{
	public static bool TryResolve(
		string? skuId,
		[NotNullWhen(true)] out JduMediaClientProfile? profile)
	{
		profile = null;
		if (!TryGetGameYear(skuId, out int gameYear) || !JduApplicationClientProfiles.GameYears.Contains(gameYear))
			return false;

		JduPlatform? platform = JduAssetPlatform.GetPlatformFromSkuId(skuId);
		if (!platform.HasValue)
			return false;

		profile = new JduMediaClientProfile(
			gameYear,
			platform.Value,
			JduAssetPlatform.GetCode(platform.Value),
			JduVideoCapabilities.GetCodec(gameYear, platform.Value));
		return true;
	}

	private static bool TryGetGameYear(string? skuId, out int gameYear)
	{
		gameYear = 0;
		return skuId is { Length: >= 6 }
			&& skuId.StartsWith("jd", StringComparison.OrdinalIgnoreCase)
			&& int.TryParse(skuId.AsSpan(2, 4), out gameYear);
	}
}
