using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.Tests.Services;

public class ClientPlatformTests
{
    [Theory]
    [InlineData("UbiServices_SDK_Unity_2024.Release.18_SWITCH64", ClientPlatform.DefaultPlatform)]
    [InlineData("UbiServices_SDK_Unity_2020.Release.58_PC64_ansi_dynamic", ClientPlatform.PC)]
    public void FromUserAgent_KnownSuffix_ReturnsPlatform(string userAgent, string expectedPlatform)
    {
        Assert.Equal(expectedPlatform, ClientPlatform.FromUserAgent(userAgent));
    }

    [Theory]
    [InlineData(ClientPlatform.DefaultPlatform, ClientPlatform.DefaultPlatform)]
    [InlineData(ClientPlatform.PC, ClientPlatform.PC)]
    [InlineData(ClientPlatform.PlayStation5, ClientPlatform.PlayStation5)]
    [InlineData(ClientPlatform.XboxSeries, ClientPlatform.XboxSeries)]
    [InlineData("unknown", ClientPlatform.DefaultPlatform)]
    public void Normalize_ReturnsSupportedPlatformOrDefault(string platform, string expectedPlatform)
    {
        Assert.Equal(expectedPlatform, ClientPlatform.Normalize(platform));
    }
}
