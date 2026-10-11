using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json.Nodes;

namespace JustDanceServer.Tests.Services;

public sealed class JduAvatarCatalogTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "JustDanceServerAvatarCatalogTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Catalog_LoadsDefaultsOverridesAndNaturallyOrderedSharedSounds()
    {
        WriteGroup("KnownMap", """
			{
			  "mapName": "KnownMap",
			  "jdVersion": 2022,
			  "avatars": {
			    "18": {
			      "coach": 0,
			      "image": "Coach0.png",
			      "sound": "AVTR_Common_Girl_Swag"
			    },
			    "19": {
			      "coach": 1,
			      "image": "Coach1.png",
			      "status": "locked",
			      "unlockType": "gacha",
			      "price": 1000,
			      "relatedSong": "KnownMap"
			    }
			  }
			}
			""", "Coach0.png", "Coach1.png");
        WriteSound("AVTR_Common_Girl_Swag", "10.ogg", "2.ogg", "1.ogg");

        JduAvatarCatalog catalog = CreateCatalog(["KnownMap"]);
        await catalog.LoadData();

        Assert.Equal(2, catalog.Avatars.Count);
        Assert.Equal((uint)0, catalog.Avatars[18].Coach);
        JsonObject wire = new JduAvatarWireProjection(
            Options.Create(new UrlSettings { CDNUrl = "cdn.example.test" }))
            .Create(catalog.Avatars);
        Assert.Equal(3, wire["18"]!["status"]!.GetValue<int>());
        Assert.Equal(20, wire["18"]!["unlockType"]!.GetValue<int>());
        Assert.Equal(1, wire["18"]!["visibility"]!.GetValue<int>());
        Assert.Equal("KnownMap", wire["18"]!["UsedAsCoach_MapName"]!.GetValue<string>());
        Assert.Equal(
            ["1.ogg", "2.ogg", "10.ogg"],
            wire["18"]!["sounds"]!.AsArray()
                .Select(value => Path.GetFileName(new Uri(value!.GetValue<string>()).AbsolutePath)));
        Assert.Equal(1, wire["19"]!["status"]!.GetValue<int>());
        Assert.Equal(18, wire["19"]!["unlockType"]!.GetValue<int>());
        Assert.Equal(1000, wire["19"]!["mojoPrice"]!.GetValue<int>());
    }

    [Fact]
    public async Task Catalog_RejectsDuplicateStableIdsAcrossGroups()
    {
        WriteGroup("First", """
			{ "jdVersion": 2022, "avatars": { "7": { "image": "Head.png" } } }
			""", "Head.png");
        WriteGroup("Second", """
			{ "jdVersion": 2022, "avatars": { "7": { "image": "Head.png" } } }
			""", "Head.png");

        await Assert.ThrowsAsync<InvalidDataException>(CreateCatalog().LoadData);
    }

    [Fact]
    public async Task Catalog_RejectsStandaloneCoach()
    {
        WriteGroup("Standalone", """
			{ "jdVersion": 2022, "avatars": { "7": { "coach": 0, "image": "Head.png" } } }
			""", "Head.png");

        await Assert.ThrowsAsync<InvalidDataException>(CreateCatalog().LoadData);
    }

    [Fact]
    public async Task Catalog_RejectsOrphanedFiles()
    {
        WriteGroup("Standalone", """
			{ "jdVersion": 2022, "avatars": { "7": { "image": "Head.png" } } }
			""", "Head.png", "Orphan.png");

        await Assert.ThrowsAsync<InvalidDataException>(CreateCatalog().LoadData);
    }

    [Fact]
    public async Task Catalog_RejectsAnUnreferencedSoundGroup()
    {
        WriteGroup("Standalone", """
			{ "jdVersion": 2022, "avatars": { "7": { "image": "Head.png" } } }
			""", "Head.png");
        WriteSound("AVTR_Unused", "1.ogg");

        await Assert.ThrowsAsync<InvalidDataException>(CreateCatalog().LoadData);
    }

    private JduAvatarCatalog CreateCatalog(IEnumerable<string>? maps = null)
    {
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.MapToGuid).Returns(
            (maps ?? []).ToDictionary(map => map, _ => Guid.NewGuid()));

        return new(
            Options.Create(new PathSettings { JduCatalogAssetsPath = Path.Combine(root, "CatalogAssets") }),
            mapService.Object,
            NullLogger<JduAvatarCatalog>.Instance);
    }

    private void WriteGroup(string name, string manifest, params string[] files)
    {
        string directory = Path.Combine(root, "CatalogAssets", "Avatars", name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "Avatars.json"), manifest);
        foreach (string file in files)
            File.WriteAllText(Path.Combine(directory, file), file);
    }

    private void WriteSound(string family, params string[] files)
    {
        string directory = Path.Combine(root, "CatalogAssets", "Sounds", family);
        Directory.CreateDirectory(directory);
        foreach (string file in files)
            File.WriteAllText(Path.Combine(directory, file), file);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
