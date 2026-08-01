using Microsoft.AspNetCore.Http;

namespace JustDanceServer.Services;

public static class ClientPlatform
{
	public const string DefaultPlatform = "NX";
	public const string PC = "PC";
	public const string PlayStation5 = "PS5";
	public const string XboxSeries = "XboxScarlett";

	public static string FromRequest(HttpRequest request)
	{
		string? platform = FromUserAgent(request.Headers.UserAgent.ToString());
		return platform ?? DefaultPlatform;
	}

	public static string? FromUserAgent(string? userAgent)
	{
		if (string.IsNullOrWhiteSpace(userAgent))
			return null;

		string trimmedUserAgent = userAgent.Trim();

		if (trimmedUserAgent.EndsWith("SWITCH64", StringComparison.OrdinalIgnoreCase))
			return DefaultPlatform;

		if (trimmedUserAgent.EndsWith("PC64_ansi_dynamic", StringComparison.OrdinalIgnoreCase))
			return PC;

		// TODO: Add PS5 user-agent suffix once we have a capture from that client.
		// TODO: Add Xbox Series X/S user-agent suffix once we have a capture from that client.
		return null;
	}

	public static string Normalize(string? platform)
		=> platform switch
		{
			PC => PC,
			PlayStation5 => PlayStation5,
			XboxSeries => XboxSeries,
			_ => DefaultPlatform
		};
}
