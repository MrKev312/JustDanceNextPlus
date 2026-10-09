using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jd2015ClientProfiles
{
    public static readonly Guid ConstantProviderSpaceId = new("1f67bf36-73af-4e21-9b57-7bb2a6e38664");

    public static IReadOnlyList<(JduPlatform Platform, Guid ApplicationId)> Platforms { get; } =
    [
        (JduPlatform.Pc, new("061c126b-f1d9-4481-b49a-085fc4612a2a")),
        (JduPlatform.PlayStation3, new("e0ec3eef-6c54-46ae-88b8-0bbefdbae582")),
        (JduPlatform.PlayStation4, new("1180c246-7151-4c7d-95d3-301b701d521a")),
        (JduPlatform.WiiU, new("3133a1ba-bf7b-443b-9e8a-f1d5f3b2ac7b")),
        (JduPlatform.Xbox360, new("06cf6c04-feb8-404e-8cf4-2cb3a073fa48")),
        (JduPlatform.XboxOne, new("e6d674d5-947e-403d-b940-fe6703b597da"))
    ];
}
