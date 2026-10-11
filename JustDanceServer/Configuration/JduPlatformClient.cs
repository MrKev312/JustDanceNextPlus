using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public sealed record JduPlatformClient(
    JduPlatform Platform,
    Guid ApplicationId,
    Guid SpaceId,
    IReadOnlyList<string>? PlatformApplicationIds = null);
