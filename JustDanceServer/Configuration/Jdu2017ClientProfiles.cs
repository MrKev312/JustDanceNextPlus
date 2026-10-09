using JustDanceServer.Services;

namespace JustDanceServer.Configuration;

public static class Jdu2017ClientProfiles
{
    public static ClassicJduGameClients Game { get; } = new(2017,
    [
        new(JduPlatform.Pc,
            new("341789d4-b41f-4f40-ac79-e2bc4c94ead4"), new("f1ae5b84-db7c-481e-9867-861cf1852dc8")),
        new(JduPlatform.PlayStation4,
            new("d03d28dd-4706-4808-ba6d-13f43ba62a11"), new("56151e14-8ba6-4baf-ba5b-cbd75bff1dcb")),
        new(JduPlatform.WiiU,
            new("81cf5bb0-e037-4374-b38d-106bd21307fa"), new("96f82187-4d45-4544-9718-0cde2f8d5ccb")),
        new(JduPlatform.XboxOne,
            new("155d58d0-94ae-4de2-b8f9-64ed5f299545"), new("b7ef9d86-2a78-4aab-97c0-d027560e4373")),
        new(JduPlatform.NintendoSwitch,
            new("613fe1a7-86c6-4fa7-bfb3-230e92ae29d3"), new("0415144b-ed4c-4098-a50d-5ba36d7d8466"), ["0100bce000598000"])
    ]);
}
