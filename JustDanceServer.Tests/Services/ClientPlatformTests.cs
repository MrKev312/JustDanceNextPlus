using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;

namespace JustDanceServer.Tests.Services;

public class ClientPlatformTests
{
	[Theory]
	[InlineData("live_1.0.0-12345_NX_Final_test_221122", true)]
	[InlineData("live_1.1.0-94591_Win_Release_obvious-assistant_230203", true)]
	[InlineData("live_2.0.0-12345_NX_Final_test_231122", false)]
	[InlineData("live_26.0.2-348122_NX_Final_near-condition_251028", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void JdNextPreviewLayout_IdentifiesOnlyJd2023Builds(string? buildId, bool expected)
	{
		Assert.Equal(expected, JdNextPreviewLayout.IsJd2023Build(buildId));
	}

	[Theory]
	[InlineData("live_1.1.0-94591_Win_Release_obvious-assistant_230203", "JDUnlimited")]
	[InlineData("live_2.0.0-12345_NX_Final_test_231122", "JDPlus")]
	[InlineData("live_26.0.2-348122_NX_Final_near-condition_251028", "JDPlus")]
	[InlineData("", "JDPlus")]
	public void JdNextPreviewLayout_SelectsMediaForClientGeneration(
		string buildId,
		string expectedLayout)
	{
		DefaultHttpContext context = new();
		context.Request.Headers["ubi-appbuildid"] = buildId;

		Assert.Equal(expectedLayout, JdNextPreviewLayout.FromRequest(context.Request).FolderName);
	}

    [Theory]
    [InlineData("UbiServices_SDK_Unity_2024.Release.18_SWITCH64", ClientPlatform.NintendoSwitch)]
    [InlineData("UbiServices_SDK_Unity_2020.Release.58_PC64_ansi_dynamic", ClientPlatform.PC)]
    public void FromUserAgent_KnownSuffix_ReturnsPlatform(string userAgent, string expectedPlatform)
    {
        Assert.Equal(expectedPlatform, ClientPlatform.FromUserAgent(userAgent));
    }

    [Theory]
    [InlineData(ClientPlatform.NintendoSwitch, ClientPlatform.NintendoSwitch)]
    [InlineData("switch", ClientPlatform.NintendoSwitch)]
    [InlineData(ClientPlatform.PC, ClientPlatform.PC)]
    [InlineData(ClientPlatform.PlayStation5, ClientPlatform.PlayStation5)]
    [InlineData(ClientPlatform.XboxSeries, ClientPlatform.XboxSeries)]
    public void Normalize_ReturnsExplicitSupportedPlatform(string platform, string expectedPlatform)
    {
        Assert.Equal(expectedPlatform, ClientPlatform.Normalize(platform));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void Normalize_UnsupportedPlatform_Throws(string? platform)
    {
        Assert.Throws<NotSupportedException>(() => ClientPlatform.Normalize(platform));
    }

    [Fact]
    public void FromRequest_MissingPlatform_ThrowsInsteadOfAssumingSwitch()
    {
        DefaultHttpContext context = new();

        Assert.Throws<BadHttpRequestException>(() => ClientPlatform.FromRequest(context.Request));
    }

	[Theory]
	[InlineData(ClientPlatform.PC, MapVideoCodec.Vp8)]
	[InlineData(ClientPlatform.NintendoSwitch, MapVideoCodec.Vp9)]
	[InlineData(ClientPlatform.PlayStation5, MapVideoCodec.Vp9)]
	[InlineData(ClientPlatform.XboxSeries, MapVideoCodec.Vp9)]
	public void GetVideoCodec_ReturnsExplicitPlatformCapability(string platform, MapVideoCodec expected)
	{
		Assert.Equal(expected, ClientPlatform.GetVideoCodec(platform));
	}
}
