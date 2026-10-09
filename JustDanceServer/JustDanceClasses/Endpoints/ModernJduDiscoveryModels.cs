namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record ModernJduServerInfo
{
    public string Name { get; init; } = "JMCS PROD";
    public required string Url { get; init; }
}

public sealed record ModernJduServerEntity
{
    public required Guid EntityId { get; init; }
    public required Guid SpaceId { get; init; }
    public string Type { get; init; } = "server";
    public string Name { get; init; } = "default";
    public uint Revision { get; init; } = 3;
    public string[] Tags { get; init; } = [];
    public required ModernJduServerInfo Obj { get; init; }
    public string LastModified { get; init; } = string.Empty;
}
