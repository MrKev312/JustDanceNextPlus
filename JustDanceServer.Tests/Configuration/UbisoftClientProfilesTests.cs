using JustDanceServer.Configuration;
using JustDanceServer.Services;

namespace JustDanceServer.Tests.Configuration;

public sealed class UbisoftClientProfilesTests
{
    [Fact]
    public void Jd2014AndJd2015RemainUnregistered()
    {
        Assert.DoesNotContain(2014, JduApplicationClientProfiles.GameYears);
        Assert.DoesNotContain(2015, JduApplicationClientProfiles.GameYears);

        Assert.Equal(6, Jd2014ClientProfiles.Platforms.Count);
        Assert.Equal(6, Jd2015ClientProfiles.Platforms.Count);
        Assert.All(Jd2014ClientProfiles.Platforms.Concat(Jd2015ClientProfiles.Platforms), platform =>
        {
            Assert.False(UbisoftApplicationProfiles.TryGetSessionIdentity(platform.ApplicationId, out _));
            Assert.False(UbisoftApplicationProfiles.TryGetApplicationTarget(platform.ApplicationId, out _));
        });
    }

    [Fact]
    public void ModernJduCatalogIsComposedFromCompleteGameYears()
    {
        Assert.Equal([2019, 2020, 2021, 2022],
            ModernJduClientProfiles.Games.Select(game => game.GameYear));
        Assert.Equal(
            ModernJduClientProfiles.Games.Sum(game => game.Platforms.Count),
            ModernJduClientProfiles.All.Count);

        Assert.Equal(
            [JduPlatform.Pc, JduPlatform.PlayStation4, JduPlatform.XboxOne,
                JduPlatform.WiiU, JduPlatform.NintendoSwitch],
            Jdu2019ClientProfiles.Game.Platforms.Select(client => client.Platform));
        ModernJduClientProfile jdu2022 = Assert.Single(
            ModernJduClientProfiles.All,
            profile => profile.ApplicationId == Guid.Parse("34ad0f04-b141-4793-bdd4-985a9175e70d"));
        Assert.Equal(Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca"), jdu2022.TargetSpaceId);
        Assert.True(jdu2022.UseDiscoveryHost);
    }

    [Theory]
    [InlineData(2016, "3d4a8d63-e473-4c70-a24d-6cf36cfc1eae", "4b48d115-222a-4112-bcfd-237407670305", "PC", "uplay")]
    [InlineData(2016, "cd0fcaad-e5e0-4996-a13a-5922362ced7a", "07e0f404-d897-4e22-a98e-e20aa7ef5c1c", "ORBIS", "psn")]
    [InlineData(2016, "2714264f-9d54-4810-845f-3f5c2135f965", "04ed0716-b067-4e11-a643-6b596242de55", "DURANGO", "xbl")]
    [InlineData(2017, "341789d4-b41f-4f40-ac79-e2bc4c94ead4", "f1ae5b84-db7c-481e-9867-861cf1852dc8", "PC", "uplay")]
    [InlineData(2017, "d03d28dd-4706-4808-ba6d-13f43ba62a11", "56151e14-8ba6-4baf-ba5b-cbd75bff1dcb", "ORBIS", "psn")]
    [InlineData(2017, "155d58d0-94ae-4de2-b8f9-64ed5f299545", "b7ef9d86-2a78-4aab-97c0-d027560e4373", "DURANGO", "xbl")]
    [InlineData(2019, "f52352a5-e2df-4f3a-bdc2-eecdce19dcf1", "02a96184-5380-460b-ac5e-9dd14abf9e94", "PC", "uplay")]
    [InlineData(2019, "46f44bb2-76f0-450b-8114-280e8b12b013", "9c029e8a-f03a-4743-a924-790dfd23e839", "ORBIS", "psn")]
    [InlineData(2019, "f64e368b-88c7-49e7-bc57-f3cfee43d70f", "97758012-ec0d-4d96-9d84-391f758e7179", "DURANGO", "xbl")]
    [InlineData(2021, "c8cfd4b7-91b0-446b-8e3b-7edfa393c946", "d85e86e5-f68b-4cf4-a8ba-ce420765e7ee", "PC", "uplay")]
    [InlineData(2021, "63475868-1e27-45c2-95c4-cfc58c951016", "d8304bc2-848b-46f5-bf75-2dfb1f6d3ddd", "ORBIS", "psn")]
    [InlineData(2021, "f78cbbdb-72eb-47f4-af54-91618c1eecd0", "7aa2ea90-484c-45eb-9799-c0ab64e8a483", "DURANGO", "xbl")]
    [InlineData(2021, "984a17fd-1da1-4f16-97ff-98d56a6c7147", "063af0d4-a405-4f44-b09d-261fa8d6d7b9", "GGP", "googlestream")]
    [InlineData(2021, "76570aa8-9ce4-43b3-b226-bcf654b5d9a8", "01d77265-3270-4cfc-91b3-0061c4c7a76e", "PROSPERO", "psn")]
    [InlineData(2021, "8375910d-7f13-441b-98db-b4065b043ae5", "1aaaf1e4-2650-485b-bbe6-0fb3eca28676", "SCARLETT", "xbl")]
    [InlineData(2022, "3cf68bce-982d-49a1-9cca-486c21192f2b", "72c286f3-89a5-432a-a229-c4a432dfb4c3", "PC", "uplay")]
    [InlineData(2022, "6e8a0cf3-c60e-44da-984c-7f8d7e1f4157", "a1f251ad-509e-4e90-b9bd-66bb28ac76cd", "ORBIS", "psn")]
    [InlineData(2022, "594d5ecc-0c64-441b-b129-42ceafb22c81", "24276189-f66e-4bd2-82bb-1fc38e7c4574", "DURANGO", "xbl")]
    [InlineData(2022, "92f0314a-a84a-441d-a335-683a1a52e058", "fb500a0b-3157-43ec-90bd-82fb8da64bd3", "GGP", "googlestream")]
    [InlineData(2022, "812a8fa1-e45a-4734-835e-6359ab86df8b", "d9e7826a-e250-406a-a64d-12d91f447d45", "PROSPERO", "psn")]
    [InlineData(2022, "6fc1c284-c2a6-4eed-9da8-c49769506fa0", "259887b7-29eb-41ae-8217-c950f098eb2d", "SCARLETT", "xbl")]
    public void AdditionalJduClientsUseExactApplicationAndSessionSpaces(
        int gameYear,
        string applicationIdText,
        string spaceIdText,
        string assetPlatform,
        string platformType)
    {
        Guid applicationId = Guid.Parse(applicationIdText);
        Guid spaceId = Guid.Parse(spaceIdText);
        IUbisoftClientProfile client = Assert.Single(
            JduApplicationClientProfiles.All,
            profile => profile.ApplicationId == applicationId);

        Assert.Equal(spaceId, client.SpaceId);
        Assert.Equal(platformType, client.PlatformType);
        if (gameYear < ModernJduConstants.FirstGameYear)
        {
            Assert.True(ClassicJduClientProfiles.TryGetBySpaceId(spaceId, out ClassicJduClientProfile? classic));
            Assert.Same(client, classic);
            Assert.Equal(gameYear, classic.GameYear);
            Assert.Equal(assetPlatform, classic.AssetPlatform);
            Assert.True(ClassicJduClientProfiles.TryGetBySkuId(
                $"jd{gameYear}-{assetPlatform.ToLowerInvariant()}-all", out ClassicJduClientProfile? fromSku));
            Assert.Same(classic, fromSku);
        }
        else
        {
            Assert.True(ModernJduClientProfiles.TryGetBySpaceId(spaceId, out ModernJduClientProfile? modern));
            Assert.Same(client, modern);
            Assert.Equal(gameYear, modern.GameYear);
            Assert.Equal(assetPlatform, modern.Platform);
        }
    }

    [Theory]
    [InlineData(2021, "59f82720-778f-427a-a0bd-e81cb112c044")]
    [InlineData(2022, "041c03fa-1735-4ea7-b5fc-c16546d092ca")]
    public void ModernPlatformsHaveIndependentSessionSpacesAndOneSharedGameSpace(
        int gameYear,
        string gameSpaceIdText)
    {
        Guid gameSpaceId = Guid.Parse(gameSpaceIdText);
        ModernJduClientProfile[] clients = [.. ModernJduClientProfiles.All.Where(client => client.GameYear == gameYear)];

        Assert.Equal(7, clients.Length);
        Assert.Equal(7, clients.Select(client => client.SpaceId).Distinct().Count());
        Assert.Equal(7, clients.Select(client => client.ClientPlatform).Distinct().Count());
        Assert.True(ModernJduClientProfiles.IsKnownSpaceId(gameSpaceId));
        Assert.False(ModernJduClientProfiles.TryGetBySpaceId(gameSpaceId, out _));
        Assert.All(clients, client =>
        {
            Assert.NotEqual(gameSpaceId, client.SpaceId);
            Assert.Equal(gameSpaceId, client.TargetSpaceId);
            Assert.True(client.UseDiscoveryHost);
        });
        Assert.Equal(clients.Select(client => client.ApplicationId),
            UbisoftApplicationProfiles.GetApplicationIdsForTarget(gameSpaceId));
    }

    [Fact]
    public void Jdu2021SwitchKeepsItsSessionSpaceSeparateFromItsGameSpace()
    {
        Assert.True(UbisoftApplicationProfiles.TryGetSessionIdentity(
            Guid.Parse("778c0fff-db09-4c65-bcf0-84a40170c1d2"), out UbisoftSessionIdentity? session));
        Assert.Equal(Guid.Parse("cd052712-ba1d-453a-89b9-08778888d380"), session.SpaceId);
    }

    [Theory]
    [InlineData("jd2021-ggp-all", JduPlatform.Stadia, "GGP", "googlestream")]
    [InlineData("jd2022-stadia-all", JduPlatform.Stadia, "GGP", "googlestream")]
    [InlineData("jd2021-scarlett-all", JduPlatform.XboxSeries, "SCARLETT", "xbl")]
    [InlineData("jd2022-xboxseries-all", JduPlatform.XboxSeries, "SCARLETT", "xbl")]
    public void AdditionalPlatformNamesResolveConsistently(
        string skuId,
        JduPlatform platform,
        string assetPlatform,
        string platformType)
    {
        Assert.True(JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? client));
        Assert.Equal(platform, client.Platform);
        Assert.Equal(assetPlatform, client.AssetPlatform);
        Assert.Equal(platformType, platform.GetUbisoftPlatformType());
        Assert.True(JduAssetPlatform.TryParse(assetPlatform, out JduPlatform parsed));
        Assert.Equal(platform, parsed);
    }

    [Fact]
    public void JdNextPcAndSwitchAreIndependentClientsOfTheSameGameSpace()
    {
        IUbisoftClientProfile pc = Assert.Single(
            UbisoftClientProfiles.All,
            profile => profile.ApplicationId == JdNextConstants.PcApplicationId);
        IUbisoftClientProfile @switch = Assert.Single(
            UbisoftClientProfiles.All,
            profile => profile.ApplicationId == JdNextConstants.SwitchApplicationId);

        Assert.Equal(JdNextConstants.SessionSpaceId, pc.SpaceId);
        Assert.Equal(JdNextConstants.SessionSpaceId, @switch.SpaceId);
        Assert.Equal(JdNextConstants.GameSpaceId, pc.TargetSpaceId);
        Assert.Equal(JdNextConstants.GameSpaceId, @switch.TargetSpaceId);
        Assert.Null(pc.PlatformApplicationIds);
        Assert.Equal([JdNextConstants.SwitchTitleId], @switch.PlatformApplicationIds);
        Assert.Equal(
            [JdNextConstants.PcApplicationId, JdNextConstants.SwitchApplicationId],
            UbisoftApplicationProfiles.GetApplicationIdsForTarget(JdNextConstants.GameSpaceId));
    }

    [Fact]
    public void ApplicationRegistryIsProjectedFromEveryClientProfile()
    {
        Assert.Equal(
            UbisoftClientProfiles.All.Count,
            UbisoftClientProfiles.All.Select(profile => profile.ApplicationId).Distinct().Count());

        foreach (IUbisoftClientProfile client in UbisoftClientProfiles.All)
        {
            Assert.True(UbisoftApplicationProfiles.TryGetSessionIdentity(
                client.ApplicationId,
                out UbisoftSessionIdentity? session));
            Assert.Equal(client.SpaceId, session.SpaceId);
            Assert.Equal(client.PlatformType, session.PlatformType);
            Assert.Equal(client.PlatformApplicationIds, session.PlatformApplicationIds);

            bool hasTarget = UbisoftApplicationProfiles.TryGetApplicationTarget(
                client.ApplicationId,
                out UbisoftApplicationTarget? target);
            Assert.Equal(client.TargetSpaceId.HasValue, hasTarget);
            if (client.TargetSpaceId is Guid targetSpaceId)
            {
                Assert.Equal(targetSpaceId, target!.SpaceId);
                Assert.Equal(client.UseDiscoveryHost, target.UseDiscoveryHost);
            }
        }
    }
}
