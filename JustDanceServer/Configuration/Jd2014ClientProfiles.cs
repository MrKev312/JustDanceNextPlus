using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jd2014ClientProfiles
{
    public static IReadOnlyList<(JduPlatform Platform, Guid ApplicationId)> Platforms { get; } =
    [
        (JduPlatform.Pc, new("c6c96b8c-926e-4a4e-8f68-d34ade1036bc")),
        (JduPlatform.PlayStation3, new("b674112e-0e1d-4009-8419-ee7c80a71ad7")),
        (JduPlatform.PlayStation4, new("3c6ef3f8-36a2-4944-86b2-7b90c145b4e1")),
        (JduPlatform.WiiU, new("1e18cb3c-c140-4e30-990e-bd744711ce5f")),
        (JduPlatform.Xbox360, new("67dd5eab-657a-4934-b423-fda24478c334")),
        (JduPlatform.XboxOne, new("ca3dfb37-f564-4206-9f77-a96b95e5325b"))
    ];
}
