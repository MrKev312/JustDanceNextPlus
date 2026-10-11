using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;
using JustDanceServer.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json.Nodes;

namespace JustDanceServer.Tests.Services;

public sealed class JduContentCatalogTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "JustDanceServerJduContentCatalogTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CustomizableItems_LoadDefaultsAndFixedAssetRoles()
    {
        WriteGroup("Skins", "First", "Skin.json", """
			{ "id": 7, "jdVersion": 2018 }
			""", "Skin.png");
        WriteGroup("PortraitBorders", "First", "Border.json", """
			{ "id": 9, "visibility": 1 }
			""", "Background.png", "Foreground.png");

        JduCustomizableItemCatalog catalog = new(
            CreatePaths(),
            NullLogger<JduCustomizableItemCatalog>.Instance);
        await catalog.LoadData();

        Assert.Equal(JduCustomizableItemStatus.Unlocked, catalog.Skins[7].Status);
        Assert.Equal(JduCustomizableItemUnlockType.UnlockedByDefault, catalog.Skins[7].UnlockType);
        Assert.EndsWith("Skins/First/Skin.png", catalog.Skins[7].ImagePath);
        Assert.EndsWith("PortraitBorders/First/Foreground.png", catalog.PortraitBorders[9].ForegroundPath);

        JduCatalogWireProjection projection = CreateProjection();
        Assert.Equal(20, projection.CreateSkins(catalog.Skins)["7"]!["unlockType"]!.GetValue<int>());
        Assert.Equal(
            "https://cdn.example.test/JDU/CatalogAssets/PortraitBorders/First/Background.png",
            projection.CreatePortraitBorders(catalog.PortraitBorders)["9"]!["backgroundUrl"]!.GetValue<string>());
    }

    [Fact]
    public async Task CustomizableItems_RejectDuplicateStableIds()
    {
        WriteGroup("Skins", "First", "Skin.json", """{ "id": 7, "jdVersion": 2018 }""", "Skin.png");
        WriteGroup("Skins", "Second", "Skin.json", """{ "id": 7, "jdVersion": 2018 }""", "Skin.png");
        WriteGroup("PortraitBorders", "First", "Border.json", """{ "id": 9, "visibility": 1 }""", "Background.png");

        JduCustomizableItemCatalog catalog = new(CreatePaths(), NullLogger<JduCustomizableItemCatalog>.Instance);
        await Assert.ThrowsAsync<InvalidDataException>(catalog.LoadData);
    }

    [Fact]
    public async Task Aliases_CombineDescriptorAndUnlockData()
    {
        WriteGroup("Aliases", "Rookie", "Alias.json", """
			{
			  "id": 1,
			  "difficultyColor": 0,
			  "restrictedToUnlimitedSongs": false,
			  "stringLocId": 15022,
			  "placeholder": "The Rookie",
			  "localized": "The Rookie",
			  "localizedFemale": "The Rookie",
			  "description": "Description",
			  "visibility": 1,
			  "unlock": {
			    "class": "JD_ObjectiveDesc",
			    "description": 14977,
			    "minimumValue": 1,
			    "objectiveType": 51
			  }
			}
			""");

        JduAliasCatalog catalog = new(CreatePaths(), NullLogger<JduAliasCatalog>.Instance);
        await catalog.LoadData();
        JsonObject wire = CreateProjection().CreateAliases(catalog.Aliases);

        Assert.Equal("The Rookie", wire["1"]!["StringPlaceholder"]!.GetValue<string>());
        Assert.Equal(51, wire["1"]!["UnlockObjective"]!["ObjectiveType"]!.GetValue<int>());
    }

    [Fact]
    public async Task Quests_ResolveOneSelfContainedGroupForEveryPlatform()
    {
        string group = WriteGroup("Quests", "Throne", "Quest.json", """
			{ "id": 2, "title": "Throne", "maps": ["KnownMap"] }
			""", "Phone.png");
        foreach (string platform in JduAssetPlatform.SupportedPlatforms)
        {
            string platformRoot = Path.Combine(group, "Assets", platform);
            Directory.CreateDirectory(platformRoot);
            foreach (string file in new[] { "Cover.ckd", "Logo.ckd", "ShadedLogo.ckd" })
                File.WriteAllText(Path.Combine(platformRoot, file), file);
        }

        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.MapToGuid).Returns(
            new Dictionary<string, Guid> { ["KnownMap"] = Guid.NewGuid() });
        JduQuestCatalog catalog = new(
            CreatePaths(),
            mapService.Object,
            NullLogger<JduQuestCatalog>.Instance);
        await catalog.LoadData();

        JsonArray wire = CreateProjection().CreateQuests(catalog.Quests, "NX");
        Assert.Equal("2", wire[0]!["id"]!.GetValue<string>());
        Assert.Equal(
            "https://cdn.example.test/JDU/CatalogAssets/Quests/Throne/Assets/NX/Cover.ckd",
            wire[0]!["assetUrls"]!["coverImageURL"]!.GetValue<string>());

        JsonObject carousel = new()
        {
            ["categories"] = new JsonArray(new JsonObject
            {
                ["isc"] = "quest_row_unlimited",
                ["items"] = new JsonArray()
            })
        };
        CreateProjection().PopulateQuestCarousel(carousel, catalog.Quests);
        Assert.Equal(
            ["2", "1"],
            carousel["categories"]![0]!["items"]!.AsArray()
                .Select(item => item!["components"]![0]!["questId"]!.GetValue<string>()));
    }

    private IOptions<PathSettings> CreatePaths()
        => Options.Create(new PathSettings { JduCatalogAssetsPath = Path.Combine(root, "CatalogAssets") });

    private static JduCatalogWireProjection CreateProjection()
        => new(Options.Create(new UrlSettings { CDNUrl = "cdn.example.test" }));

    private string WriteGroup(
        string family,
        string group,
        string manifestName,
        string manifest,
        params string[] files)
    {
        string directory = Path.Combine(root, "CatalogAssets", family, group);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, manifestName), manifest);
        foreach (string file in files)
            File.WriteAllText(Path.Combine(directory, file), file);
        return directory;
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
