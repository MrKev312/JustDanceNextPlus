namespace JustDanceServer.Services;

public static class JdNextPreviewLayout
{
	private const string BuildIdHeader = "ubi-appbuildid";

	public static MapMediaLayout FromRequest(HttpRequest request)
	{
		ArgumentNullException.ThrowIfNull(request);

		// JD2023 knowingly uses the cropped UbiArt preview because it fits that client's
		// preview viewport; JD2024 and later use the native JDNext preview.
		return IsJd2023Build(request.Headers[BuildIdHeader].ToString())
			? MapMediaLayouts.JDUnlimited
			: MapMediaLayouts.JDPlus;
	}

	internal static bool IsJd2023Build(string? buildId)
	{
		if (string.IsNullOrWhiteSpace(buildId))
			return false;

		const string prefix = "live_";
		string value = buildId.Trim();
		if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			return false;

		int versionEnd = value.IndexOf('-', prefix.Length);
		string versionText = versionEnd < 0
			? value[prefix.Length..]
			: value[prefix.Length..versionEnd];

		return Version.TryParse(versionText, out Version? version)
			&& version.Major == 1;
	}
}
