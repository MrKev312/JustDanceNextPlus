namespace JustDanceServer.Configuration;

public sealed class ClassicJduDiagnosticsSettings
{
	public string AssetOverrideMapName { get; set; } = string.Empty;
	public Dictionary<string, string> AssetOverrides { get; set; } = [with(StringComparer.Ordinal)];
}
