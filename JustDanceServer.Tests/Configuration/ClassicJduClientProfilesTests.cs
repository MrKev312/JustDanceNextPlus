using JustDanceServer.Configuration;
using JustDanceServer.Services;

namespace JustDanceServer.Tests.Configuration;

public sealed class ClassicJduClientProfilesTests
{
    [Fact]
    public void RegistryIsExpandedFromPerGameDefinitions()
    {
        Assert.Equal([2016, 2017, 2018], ClassicJduClientProfiles.Games.Select(game => game.GameYear));
        Assert.Equal(
            ClassicJduClientProfiles.Games.Sum(game => game.Platforms.Count),
            ClassicJduClientProfiles.All.Count);

        foreach (ClassicJduGameClients game in ClassicJduClientProfiles.Games)
        {
            Assert.True(ClassicJduClientProfiles.TryGetGameYear($"JD{game.GameYear}-unknown-all", out int gameYear));
            Assert.Equal(game.GameYear, gameYear);

            foreach (JduPlatformClient platform in game.Platforms)
            {
                ClassicJduClientProfile client = ClassicJduClientProfiles.GetClient(game.GameYear, platform.Platform);
                Assert.Equal(platform.ApplicationId, client.ApplicationId);
                Assert.Equal(platform.SpaceId, client.SpaceId);
                Assert.Equal(platform.PlatformApplicationIds, client.PlatformApplicationIds);
                Assert.True(ClassicJduClientProfiles.TryGetByApplicationId(client.ApplicationId, out ClassicJduClientProfile? byApplication));
                Assert.True(ClassicJduClientProfiles.TryGetBySpaceId(client.SpaceId, out ClassicJduClientProfile? bySpace));
                Assert.True(ClassicJduClientProfiles.TryGetBySkuId(
                    $"JD{game.GameYear}-{client.AssetPlatform}-all", out ClassicJduClientProfile? bySku));
                Assert.Same(client, byApplication);
                Assert.Same(client, bySpace);
                Assert.Same(client, bySku);
            }
        }
    }

    [Theory]
    [InlineData(JduPlatform.Pc, "PC", "uplay", "PC")]
    [InlineData(JduPlatform.PlayStation4, "PS4", "psn", "ORBIS")]
    [InlineData(JduPlatform.WiiU, "WIIU", "wiiu", "WIIU")]
    [InlineData(JduPlatform.XboxOne, "XONE", "xbl", "DURANGO")]
    [InlineData(JduPlatform.NintendoSwitch, "SWITCH", "switch", "NX")]
    public void PlatformDefaultsPreserveClassicWireNames(
        JduPlatform platform,
        string configurationPlatform,
        string platformType,
        string assetPlatform)
    {
        ClassicJduClientProfile[] clients =
            [.. ClassicJduClientProfiles.All.Where(client => client.ClientPlatform == platform)];

        Assert.NotEmpty(clients);
        Assert.All(clients, client =>
        {
            Assert.Equal(configurationPlatform, client.Platform);
            Assert.Equal(platformType, client.PlatformType);
            Assert.Equal(assetPlatform, client.AssetPlatform);
        });
    }

    [Fact]
    public void GameDefinitionExpandsNewYearsWithoutPlatformBoilerplate()
    {
        Guid applicationId = Guid.NewGuid();
        Guid spaceId = Guid.NewGuid();
        ClassicJduGameClients game = new(2030,
        [
            new(JduPlatform.NintendoSwitch, applicationId, spaceId, ["test-title"])
        ]);

        ClassicJduClientProfile client = Assert.Single(game.Expand());

        Assert.Equal(2030, client.GameYear);
        Assert.Equal(applicationId, client.ApplicationId);
        Assert.Equal(spaceId, client.SpaceId);
        Assert.Equal(JduPlatform.NintendoSwitch, client.ClientPlatform);
        Assert.Equal("SWITCH", client.Platform);
        Assert.Equal("switch", client.PlatformType);
        Assert.Equal("NX", client.AssetPlatform);
        Assert.Equal(["test-title"], client.PlatformApplicationIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("jd2016")]
    [InlineData("jd2016_nx-all")]
    [InlineData("jd20160-nx-all")]
    [InlineData("jdABCD-nx-all")]
    [InlineData("jd9999-nx-all")]
    [InlineData("jd2022-nx-all")]
    public void InvalidOrNonClassicSkusDoNotResolve(string? skuId)
    {
        Assert.False(ClassicJduClientProfiles.IsClassicSku(skuId));
        Assert.False(ClassicJduClientProfiles.TryGetGameYear(skuId, out int gameYear));
        Assert.Equal(0, gameYear);
        Assert.False(ClassicJduClientProfiles.TryGetBySkuId(skuId, out ClassicJduClientProfile? client));
        Assert.Null(client);
    }

    [Fact]
    public void RecognizingAYearDoesNotInventAnApplicationForAnUnregisteredPlatform()
    {
        Assert.True(ClassicJduClientProfiles.IsClassicSku("jd2016-nx-all"));
        Assert.False(ClassicJduClientProfiles.TryGetBySkuId("jd2016-nx-all", out ClassicJduClientProfile? client));
        Assert.Null(client);
        Assert.Throws<KeyNotFoundException>(() => ClassicJduClientProfiles.GetClient(2016, JduPlatform.NintendoSwitch));
    }

    [Fact]
    public void MediaYearsFollowTheCombinedGameRegistry()
    {
        int[] gameYears =
        [
            .. ClassicJduClientProfiles.Games.Select(game => game.GameYear),
            .. ModernJduClientProfiles.Games.Select(game => game.GameYear)
        ];
        Assert.Equal(gameYears.Order(), JduApplicationClientProfiles.GameYears.Order());
        Assert.All(gameYears, gameYear =>
        {
            Assert.True(JduMediaClientProfiles.TryResolve($"jd{gameYear}-nx-all", out JduMediaClientProfile? media));
            Assert.Equal(gameYear, media.GameYear);
        });
        Assert.False(JduMediaClientProfiles.TryResolve("jd9999-nx-all", out _));
    }
}
