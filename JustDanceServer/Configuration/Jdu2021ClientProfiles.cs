using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2021ClientProfiles
{
    public static ModernJduGameClients Game { get; } = new(2021,
    [
        new(JduPlatform.Pc,
            new("c8cfd4b7-91b0-446b-8e3b-7edfa393c946"), new("d85e86e5-f68b-4cf4-a8ba-ce420765e7ee")),
        new(JduPlatform.PlayStation4,
            new("63475868-1e27-45c2-95c4-cfc58c951016"), new("d8304bc2-848b-46f5-bf75-2dfb1f6d3ddd")),
        new(JduPlatform.XboxOne,
            new("f78cbbdb-72eb-47f4-af54-91618c1eecd0"), new("7aa2ea90-484c-45eb-9799-c0ab64e8a483")),
        new(JduPlatform.Stadia,
            new("984a17fd-1da1-4f16-97ff-98d56a6c7147"), new("063af0d4-a405-4f44-b09d-261fa8d6d7b9")),
        new(JduPlatform.PlayStation5,
            new("76570aa8-9ce4-43b3-b226-bcf654b5d9a8"), new("01d77265-3270-4cfc-91b3-0061c4c7a76e")),
        new(JduPlatform.XboxSeries,
            new("8375910d-7f13-441b-98db-b4065b043ae5"), new("1aaaf1e4-2650-485b-bbe6-0fb3eca28676")),
        new(JduPlatform.NintendoSwitch,
            new("778c0fff-db09-4c65-bcf0-84a40170c1d2"), new("cd052712-ba1d-453a-89b9-08778888d380"), ["0100bf9011b52000"])
    ], new Guid("59f82720-778f-427a-a0bd-e81cb112c044"), UseDiscoveryHost: true);
}
