using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2018ClientProfiles
{
    public static ClassicJduGameClients Game { get; } = new(2018,
    [
        new(JduPlatform.Pc,
            new("740a6dc8-7d7a-4fbe-be2c-aa5d8c65c5e8"), new("13b9ffa4-c16a-431e-9209-7a3d5ad32c9d")),
        new(JduPlatform.PlayStation4,
            new("338f2a70-2587-44ee-a64b-5dbe112fb043"), new("16246b37-d8f2-43b4-a5ba-f07c2ed5a88e")),
        new(JduPlatform.WiiU,
            new("f8c1989f-ea92-4567-b43d-436de74b2c5d"), new("807358a9-c9e5-4d5b-be84-e5e7b98d0421")),
        new(JduPlatform.XboxOne,
            new("ccfda907-9767-4ae5-895c-6204e7a39cbd"), new("947bcde1-c49f-4017-b073-458ecb7cefef")),
        new(JduPlatform.NintendoSwitch,
            new("853badc5-e5b8-47a9-be06-630a1071701c"), new("cdf9d0d7-8e4d-4afa-8268-c25ad42c579b"), ["0100a0500348a000"])
    ]);
}
