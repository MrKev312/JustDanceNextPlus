using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2016ClientProfiles
{
    public static ClassicJduGameClients Game { get; } = new(2016,
    [
        new(JduPlatform.Pc,
            new("3d4a8d63-e473-4c70-a24d-6cf36cfc1eae"), new("4b48d115-222a-4112-bcfd-237407670305")),
        new(JduPlatform.PlayStation4,
            new("cd0fcaad-e5e0-4996-a13a-5922362ced7a"), new("07e0f404-d897-4e22-a98e-e20aa7ef5c1c")),
        new(JduPlatform.WiiU,
            new("3696a38b-1edd-4e7b-bbb7-855f15a25408"), new("17599f67-8a3b-407d-ac5e-7dfdd66d5194")),
        new(JduPlatform.XboxOne,
            new("2714264f-9d54-4810-845f-3f5c2135f965"), new("04ed0716-b067-4e11-a643-6b596242de55"))
    ]);
}
