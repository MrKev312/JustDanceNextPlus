namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record ModernJduApplicationConfigurationResponse
{
    public required ModernJduApplicationConfiguration Configuration { get; init; }
}

public sealed record ModernJduApplicationConfiguration
{
    public required ModernJduPlatformConfiguration PlatformConfig { get; init; }
    public object[] Resources { get; init; } = [];
    public object[] LegacyUrls { get; init; } = [];
    public object[] Sandboxes { get; init; } = [];
    public Dictionary<string, object> Custom { get; init; } = [];
    public ModernJduFeatureSwitch[] FeaturesSwitches { get; init; } = [];
    public ModernJduGatewayResource[] GatewayResources { get; init; } = [];
    public Dictionary<string, object> SdkConfig { get; init; } = [];
    public object[] RelatedPopulations { get; init; } = [];
}

public sealed record ModernJduPlatformConfiguration
{
    public required Guid ApplicationId { get; init; }
    public required Guid SpaceId { get; init; }
    public required string Platform { get; init; }
    public string UplayGameCode { get; init; } = string.Empty;
    public string Environment { get; init; } = "PROD";
}

public sealed record ModernJduFeatureSwitch
{
    public required string Name { get; init; }
    public bool Value { get; init; } = true;
}

public sealed record ModernJduGatewayResource
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required int Version { get; init; }
}
