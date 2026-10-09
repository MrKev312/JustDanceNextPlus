using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2020ClientProfiles
{
    public static readonly Guid SwitchJmcsConfigurationSpaceId = new("43cb82cb-1acf-4654-acf5-6095dbe76ed3");

    public static ModernJduGameClients Game { get; } = new(2020,
    [
        new(JduPlatform.NintendoSwitch,
            new("b9363005-413a-436d-a75c-99c2d7a5d0de"), new("d2aa3e85-8340-462c-bd13-3a88dcfe6a67"), ["0100ddb00db38000", "0100a8f01154e000"])
    ]);
}
