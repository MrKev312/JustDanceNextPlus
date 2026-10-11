using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2022ClientProfiles
{
    public static ModernJduGameClients Game { get; } = new(2022,
    [
        new(JduPlatform.Pc,
            new("3cf68bce-982d-49a1-9cca-486c21192f2b"), new("72c286f3-89a5-432a-a229-c4a432dfb4c3")),
        new(JduPlatform.PlayStation4,
            new("6e8a0cf3-c60e-44da-984c-7f8d7e1f4157"), new("a1f251ad-509e-4e90-b9bd-66bb28ac76cd")),
        new(JduPlatform.XboxOne,
            new("594d5ecc-0c64-441b-b129-42ceafb22c81"), new("24276189-f66e-4bd2-82bb-1fc38e7c4574")),
        new(JduPlatform.Stadia,
            new("92f0314a-a84a-441d-a335-683a1a52e058"), new("fb500a0b-3157-43ec-90bd-82fb8da64bd3")),
        new(JduPlatform.PlayStation5,
            new("812a8fa1-e45a-4734-835e-6359ab86df8b"), new("d9e7826a-e250-406a-a64d-12d91f447d45")),
        new(JduPlatform.XboxSeries,
            new("6fc1c284-c2a6-4eed-9da8-c49769506fa0"), new("259887b7-29eb-41ae-8217-c950f098eb2d")),
        new(JduPlatform.NintendoSwitch,
            new("34ad0f04-b141-4793-bdd4-985a9175e70d"), new("c0e6c86a-09ba-4b31-afa6-c6dd0759ff0d"), ["0100ea6014bb8000"])
    ], new Guid("041c03fa-1735-4ea7-b5fc-c16546d092ca"), UseDiscoveryHost: true);
}
