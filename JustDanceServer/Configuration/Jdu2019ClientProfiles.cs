using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2019ClientProfiles
{
    public static readonly Guid ServerEntityId = new("cd8593dc-2d53-449c-9dbd-cc43ebb7d3d4");

    public static ModernJduGameClients Game { get; } = new(2019,
    [
        new(JduPlatform.Pc,
            new("f52352a5-e2df-4f3a-bdc2-eecdce19dcf1"), new("02a96184-5380-460b-ac5e-9dd14abf9e94")),
        new(JduPlatform.PlayStation4,
            new("46f44bb2-76f0-450b-8114-280e8b12b013"), new("9c029e8a-f03a-4743-a924-790dfd23e839")),
        new(JduPlatform.XboxOne,
            new("f64e368b-88c7-49e7-bc57-f3cfee43d70f"), new("97758012-ec0d-4d96-9d84-391f758e7179")),
        new(JduPlatform.WiiU,
            new("3b4009a1-66cb-410d-9799-3041835d8e1b"), new("ef680ba3-e1ae-4fb5-b434-ab448f1c3210")),
        new(JduPlatform.NintendoSwitch,
            new("848e9f13-07c1-41ae-baef-57bb9f4ac899"), new("e92e485b-893e-4dd1-a55d-13d2696fcc38"), ["010075600ae96000"])
    ]);
}
