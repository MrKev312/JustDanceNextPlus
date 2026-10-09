namespace JustDanceServer.Configuration;

public sealed class PlatformAuthenticationSettings
{
	public bool AllowUnverifiedPlatformTickets { get; set; }
	public NintendoPlatformAuthenticationSettings Nintendo { get; set; } = new();
	// ASP.NET Core Identity V3 password hashes, configured through secrets/environment settings.
	public Dictionary<string, string> BasicPasswordHashes { get; set; } = [with(StringComparer.OrdinalIgnoreCase)];
}

public sealed class NintendoPlatformAuthenticationSettings
{
	public string[] ValidAudiences { get; set; } = ["ed9e2f05d286f7b8"];
}
