using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json.Nodes;

namespace JustDanceServer.Tests.Services;

public sealed class JduCatalogServiceTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "JustDanceServerTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Catalogs_MergeCommonYearAndPlatformWithoutRereadingFiles()
    {
        WriteCatalog("items", "common", "base.json", """
			{
			  "__class": "OnlineCustomizableItemDb",
			  "catalogValues": { "1": { "name": "common" } },
			  "values": [1]
			}
			""");
        string yearFile = WriteCatalog("items", "jd2018", "year.json", """
			{
			  "catalogValues": { "2": { "name": "year" } },
			  "values": [2]
			}
			""");
        WriteCatalog("items", "jd2018-nx", "platform.json", """
			{
			  "catalogValues": { "3": { "name": "platform" } },
			  "values": [3]
			}
			""");

        JduCatalogService service = CreateService();
        await service.LoadData();
        File.WriteAllText(yearFile, "not valid JSON anymore");

        Assert.True(service.TryCreateCatalog("items", "jd2018-nx-all", out JsonObject catalog));
        Assert.Equal(3, catalog["catalogValues"]!.AsObject().Count);
        Assert.Equal([1, 2, 3], catalog["values"]!.AsArray().Select(value => value!.GetValue<int>()));
    }

    [Fact]
    public async Task Catalogs_ProjectLocalUrlsToTheConfiguredSingleCdn()
    {
        string assetDirectory = Path.Combine(root, "assets");
        Directory.CreateDirectory(assetDirectory);
        File.WriteAllText(Path.Combine(assetDirectory, "abc.png"), "asset");
        WriteCatalog("items", "common", "base.json", """
			{
			  "__class": "OnlineCustomizableItemDb",
			  "catalogValues": {
			    "1": { "url": "jdu-content://abc.png" }
			  }
			}
			""");

        JduCatalogService service = CreateService();
        await service.LoadData();

        Assert.True(service.TryCreateCatalog("items", "jd2022-nx-all", out JsonObject catalog));
        Assert.Equal(
            "https://cdn.example.test/JDU/CatalogAssets/abc.png",
            catalog["catalogValues"]!["1"]!["url"]!.GetValue<string>());
    }

    [Fact]
    public async Task CatalogLoad_AllowsATrailingSeparatorOnTheAssetRoot()
    {
        string assetDirectory = Path.Combine(root, "assets");
        Directory.CreateDirectory(assetDirectory);
        File.WriteAllText(Path.Combine(assetDirectory, "abc.png"), "asset");
        WriteCatalog("items", "common", "base.json", """
			{
			  "avatars": { "1": { "url": "jdu-content://abc.png" } }
			}
			""");

        JduCatalogService service = CreateService(
            catalogAssetsPath: Path.Combine(root, "assets") + Path.DirectorySeparatorChar);
        await service.LoadData();

        Assert.True(service.TryCreateCatalog("items", null, out _));
    }

    [Fact]
    public async Task Catalogs_ProjectPlatformAssetsForEverySupportedJduPlatform()
    {
        foreach (string platform in JduAssetPlatform.SupportedPlatforms)
        {
            string assetDirectory = Path.Combine(
                root,
                "assets",
                "Quests",
                "Cooked",
                platform,
                "Covers");
            Directory.CreateDirectory(assetDirectory);
            File.WriteAllText(Path.Combine(assetDirectory, "quest_2_cover.ckd"), platform);
        }

        WriteCatalog("quests", "common", "base.json", """
			{
			  "__class": "OnlineQuestDb",
			  "platformAssets": [
			    {
			      "id": "2",
			      "assetUrls": {
			        "coverImageURL": "jdu-content://Quests/Cooked/{platform}/Covers/quest_2_cover.ckd"
			      }
			    }
			  ]
			}
			""");

        JduCatalogService service = CreateService();
        await service.LoadData();

        Dictionary<string, string> expectedPlatforms = new()
        {
            ["jd2018-pc-all"] = "PC",
            ["jd2018-wiiu-all"] = "WIIU",
            ["jd2018-orbis-all"] = "ORBIS",
            ["jd2021-prospero-all"] = "PROSPERO",
            ["jd2018-durango-all"] = "DURANGO",
            ["jd2018-nx-all"] = "NX"
        };
        foreach ((string skuId, string platform) in expectedPlatforms)
        {
            Assert.True(service.TryCreateCatalog("quests", skuId, out JsonObject catalog));
            Assert.Equal(
                $"https://cdn.example.test/JDU/CatalogAssets/Quests/Cooked/{platform}/Covers/quest_2_cover.ckd",
                catalog["platformAssets"]![0]!["assetUrls"]!["coverImageURL"]!.GetValue<string>());
        }
    }

    [Fact]
    public async Task LegacyAvatarCatalog_IsAProjectionOfTheSingleItemCatalog()
    {
        WriteCatalog("items", "common", "base.json", """
			{
			  "__class": "OnlineCustomizableItemDb",
			  "avatars": { "7": { "JdVersion": 2016 } },
			  "skins": { "8": {} }
			}
			""");

        JduCatalogService service = CreateService(
            avatarIds: [7]);
        await service.LoadData();

        Assert.True(service.TryCreateLegacyAvatarCatalog("jd2016-wiiu-all", out JsonObject catalog));
        Assert.Equal("OnlineAvatarDb", catalog["__class"]!.GetValue<string>());
        Assert.True(catalog["avatars"]!.AsObject().ContainsKey("7"));
        Assert.False(catalog.ContainsKey("skins"));
    }

    [Fact]
    public async Task CatalogLoad_FailsWhenALocalAssetIsMissing()
    {
        WriteCatalog("items", "common", "base.json", """
			{
			  "avatars": { "1": { "url": "jdu-content://missing.png" } }
			}
			""");

        JduCatalogService service = CreateService();
        await Assert.ThrowsAsync<FileNotFoundException>(service.LoadData);
    }

    [Fact]
    public async Task Catalogs_ProjectLooseObjectAndArrayEntriesToWireCollections()
    {
        WriteCatalog("items", "common", "root.json", """
			{
			  "__class": "OnlineCustomizableItemDb",
			  "catalogValues": {}
			}
			""");
        WriteCatalog("items", "common", Path.Combine("catalogValues", "custom", "7.json"), """
			{ "url": "avatar.png" }
			""");
        WriteCatalog("misc-quests", "common", "root.json", """
			{
			  "__class": "OnlineQuestDb",
			  "catalogValues": []
			}
			""");
        WriteCatalog("misc-quests", "common", Path.Combine("catalogValues", "custom", "30.json"), """
			{ "id": "30", "title": "Quest" }
			""");

        JduCatalogService service = CreateService();
        await service.LoadData();

        Assert.True(service.TryCreateCatalog("items", null, out JsonObject catalog));
        Assert.Equal("avatar.png", catalog["catalogValues"]!["7"]!["url"]!.GetValue<string>());
        Assert.True(service.TryCreateCatalog("misc-quests", null, out JsonObject quests));
        Assert.Equal("30", quests["catalogValues"]![0]!["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task Catalogs_ApplyRootEntryMetadataAfterLoadingLooseEntries()
    {
        WriteCatalog("misc", "common", "root.json", """
			{
			  "aliases": {
			    "7": {
			      "Visibility": 1,
			      "UnlockObjective": { "MinimumValue": 5 }
			    }
			  }
			}
			""");
        WriteCatalog("misc", "common", Path.Combine("aliases", "2022", "7.json"), """
			{
			  "StringPlaceholder": "The Dancer",
			  "Visibility": 0
			}
			""");

        JduCatalogService service = CreateService();
        await service.LoadData();

        Assert.True(service.TryCreateCatalog("misc", null, out JsonObject catalog));
        JsonNode alias = catalog["aliases"]!["7"]!;
        Assert.Equal("The Dancer", alias["StringPlaceholder"]!.GetValue<string>());
        Assert.Equal(1, alias["Visibility"]!.GetValue<int>());
        Assert.Equal(5, alias["UnlockObjective"]!["MinimumValue"]!.GetValue<int>());
    }

    [Fact]
    public async Task CatalogLoad_RejectsDuplicateLooseObjectKeysAcrossSubdirectories()
    {
        WriteCatalog("misc", "common", "root.json", """{ "aliases": {} }""");
        WriteCatalog("misc", "common", Path.Combine("aliases", "first", "7.json"), """{}""");
        WriteCatalog("misc", "common", Path.Combine("aliases", "second", "7.json"), """{}""");

        JduCatalogService service = CreateService();
        await Assert.ThrowsAsync<InvalidDataException>(service.LoadData);
    }

    [Fact]
    public async Task CatalogLoad_RecognizesRootFilesWhenTheConfiguredPathUsesForwardSlashes()
    {
        WriteCatalog("misc", "common", "root.json", """{ "aliases": {} }""");
        WriteCatalog("misc", "common", Path.Combine("aliases", "7.json"), """{}""");
        string configuredPath = Path.Combine(root, "catalogs").Replace('\\', '/');

        JduCatalogService service = CreateService(catalogPath: configuredPath);
        await service.LoadData();

        Assert.True(service.TryCreateCatalog("misc", null, out JsonObject catalog));
        Assert.True(catalog["aliases"]!.AsObject().ContainsKey("7"));
    }

    [Fact]
    public async Task CatalogLoad_WarnsWhenAnEntryReferencesAnUnknownMap()
    {
        WriteCatalog("items", "common", "root.json", """
			{
			  "avatars": {}
			}
			""");
        WriteCatalog("items", "common", Path.Combine("avatars", "custom", "7.json"), """
			{ "relativeSongName": "MissingMap" }
			""");
        RecordingLogger logger = new();

        JduCatalogService service = CreateService(["KnownMap"], logger);
        await service.LoadData();

        Assert.Contains(logger.Warnings, warning => warning.Contains("MissingMap"));
    }

    private JduCatalogService CreateService(
        IEnumerable<string>? mapNames = null,
        ILogger<JduCatalogService>? logger = null,
        string? catalogPath = null,
        string? catalogAssetsPath = null,
        IEnumerable<uint>? avatarIds = null)
    {
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.MapToGuid).Returns(
            (mapNames ?? []).ToDictionary(mapName => mapName, _ => Guid.NewGuid()));

        return new(
            Options.Create(new PathSettings
            {
                JduCatalogPath = catalogPath ?? Path.Combine(root, "catalogs"),
                JduCatalogAssetsPath = catalogAssetsPath ?? Path.Combine(root, "assets")
            }),
            Options.Create(new UrlSettings { CDNUrl = "cdn.example.test" }),
            mapService.Object,
            new StubAvatarCatalog(avatarIds ?? []),
            new JduAvatarWireProjection(Options.Create(new UrlSettings { CDNUrl = "cdn.example.test" })),
            new StubCustomizableItemCatalog(),
            new StubAliasCatalog(),
            new StubQuestCatalog(),
            new JduCatalogWireProjection(Options.Create(new UrlSettings { CDNUrl = "cdn.example.test" })),
            logger ?? NullLogger<JduCatalogService>.Instance);
    }

    private string WriteCatalog(string family, string scope, string fileName, string json)
    {
        string directory = Path.Combine(root, "catalogs", family, scope);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    private sealed class RecordingLogger : ILogger<JduCatalogService>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }

    private sealed class StubAvatarCatalog(IEnumerable<uint> ids) : IJduAvatarCatalog
    {
        public IReadOnlyDictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduAvatarDefinition> Avatars
        { get; } = ids.ToDictionary(
                id => id,
                id => new JustDanceServer.JustDanceClasses.Catalog.JduAvatarDefinition(
                    id,
                    2016,
                    null,
                    null,
                    "Avatars/Test/Head.png",
                    null,
                    [],
                    JustDanceServer.JustDanceClasses.Catalog.JduCustomizableItemStatus.Unlocked,
                    JustDanceServer.JustDanceClasses.Catalog.JduCustomizableItemUnlockType.UnlockedByDefault,
                    0,
                    null,
                    null,
                    null));

        public Task LoadData() => Task.CompletedTask;
    }

    private sealed class StubCustomizableItemCatalog : IJduCustomizableItemCatalog
    {
        public IReadOnlyDictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduSkinDefinition> Skins { get; }
            = new Dictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduSkinDefinition>();
        public IReadOnlyDictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduPortraitBorderDefinition> PortraitBorders { get; }
            = new Dictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduPortraitBorderDefinition>();
        public Task LoadData() => Task.CompletedTask;
    }

    private sealed class StubAliasCatalog : IJduAliasCatalog
    {
        public IReadOnlyDictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduAliasDefinition> Aliases { get; }
            = new Dictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduAliasDefinition>();
        public Task LoadData() => Task.CompletedTask;
    }

    private sealed class StubQuestCatalog : IJduQuestCatalog
    {
        public IReadOnlyDictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduQuestDefinition> Quests { get; }
            = new Dictionary<uint, JustDanceServer.JustDanceClasses.Catalog.JduQuestDefinition>();
        public Task LoadData() => Task.CompletedTask;
    }
}
