using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

using Moq;

using ClassicSongDatabaseController = JustDanceServer.Controllers.prod.just_dance.com.songdb.v1.Songs;

namespace JustDanceServer.Tests.Services;

public sealed class ClassicJduCatalogTests
{
    [Fact]
    public void SongDatabase_ProjectsSharedMapAndLinksVorbisAndVp9Previews()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        Guid primaryId = Guid.NewGuid();
        songs[primaryId] = new JustDanceSongDBEntry
        {
            Artist = "The Artist",
            AssetsMetadata = new AssetsMetadata
            {
                AudioPreviewTrk = new AudioPreviewTrk
                {
                    StartBeat = -2,
                    EndBeat = 300,
                    Markers = [0, 48_000, 96_000, 1_500_000],
                    PreviewEntry = 1
                }
            },
            CoachCount = 2,
            Credits = "The Credits",
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Classic")),
            Difficulty = 3,
            DoubleScoringType = "2",
            LyricsColor = "#AABBCCDD",
            SongColors = new Dictionary<string, string>
            {
                ["songColor_1A"] = "#FB6B6BFF",
                ["songColor_1B"] = "580448FF",
                ["songColor_2A"] = "#8D07FFFF",
                ["songColor_2B"] = "5C0F91FF"
            },
            MapLength = 123.5f,
            MapName = "Abracadabra",
            OriginalJDVersion = 2022,
            Tags = ["jdplus"],
            Title = "The Title"
        };
        songs[Guid.NewGuid()] = new JustDanceSongDBEntry
        {
            Artist = "蔡依林",
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Excluded")),
            MapName = "ExcludedMap",
            Title = "Excluded"
        };
        songs[Guid.NewGuid()] = new JustDanceSongDBEntry
        {
            Artist = "Lady Gaga",
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Classic")),
            MapName = "PokerFaceALT",
            Title = "ポーカーフェイス"
        };

        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> platforms =
            CreateClassicJduPlatforms("Abracadabra", coachCount: 2);
        MapMediaAssets jduMedia = ServerMapTestData.CreateMedia(
            MapMediaLayouts.JDUnlimited,
            platforms,
            previewAudioUrl: "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/test.ogg",
            vp9: ServerMapTestData.CreateVideoSet(
                "vp9",
                "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/vp9",
                [
                    new WebmData { Start = 640, End = 1130, Duration = 30, Bitrate = 500_000 },
                    new WebmData { Start = 641, End = 1131, Duration = 30, Bitrate = 1_500_000 }
                ],
                ["low", "mid"]));
        OrderedDictionary<Guid, ServerMap> maps = songs.ToServerMaps();
        maps[primaryId] = ServerMapTestData.CreateMap(
            primaryId,
            Path.Combine("maps", "Abracadabra"),
            songs[primaryId],
            jdUnlimited: jduMedia);
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(maps);

        ClassicJduSongDatabaseService service = new(
            mapService.Object,
            Options.Create(new UrlSettings
            {
                HostUrl = "next-service.test",
                JduHostUrl = "jdu-service.test",
                CDNUrl = "cdn.test"
            }),
            Options.Create(new ClassicJduDiagnosticsSettings()));
        ClassicJduSongDatabaseEntry song = Assert.Single(
            service.CreateDatabase(videoPreviewCodec: MapVideoCodec.Vp9, assetPlatform: "NX")).Value;

        Assert.Equal("Abracadabra", song.MapName);
        Assert.Equal("The Title", song.Title);
        Assert.Equal(2022, song.OriginalJDVersion);
        Assert.Equal(2, song.DoubleScoringType);
        Assert.Equal("FB6B6BFF", song.SongColors["songColor_1A"]);
        Assert.Equal("5C0F91FF", song.SongColors["songColor_2B"]);
        Assert.Equal("Abracadabra_mapContent", song.Packages["mapContent"]);
        Assert.Contains("Main", song.Tags);
        Assert.Contains("subscribedSong", song.Tags);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Assets/NX/Cover/cover.ckd",
            song.Assets["coverImageUrl"]);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Assets/NX/AlbumCoach/albumcoach.ckd",
            song.Assets["expandCoachImageUrl"]);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Assets/NX/BannerBackground/banner.ckd",
            song.Assets["banner_bkgImageUrl"]);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/test.ogg",
            song.Urls["jmcs://jd-contents/Abracadabra/Abracadabra_AudioPreview.ogg"]);
        Assert.Contains("codecs=\"vp9\"", song.MapPreviewMpd);
        Assert.Contains(
            "jmcs://jd-contents/Abracadabra/Abracadabra_MapPreviewNoSoundCrop_LOW.vp9.webm",
            song.MapPreviewMpd);
        Assert.NotNull(System.Xml.Linq.XDocument.Parse(song.MapPreviewMpd).Root);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/vp9/low.webm",
            song.Urls["jmcs://jd-contents/Abracadabra/Abracadabra_MapPreviewNoSoundCrop_LOW.vp9.webm"]);

        ClassicJduSongDatabaseEntry jd2017Song = Assert.Single(
            service.CreateDatabase(videoPreviewCodec: null, assetPlatform: "NX")).Value;
        Assert.Empty(jd2017Song.MapPreviewMpd);
        Assert.DoesNotContain(jd2017Song.Urls.Keys, url => url.EndsWith(".vp9.webm"));

        using System.Text.Json.JsonDocument preview = System.Text.Json.JsonDocument.Parse(song.AudioPreviewData);
        System.Text.Json.JsonElement structure = preview.RootElement.GetProperty("structure");
        Assert.Equal("MusicTrackData", preview.RootElement.GetProperty("__class").GetString());
        Assert.Equal([0, 48_000], structure.GetProperty("markers").EnumerateArray().Select(value => value.GetInt32()));
        Assert.Equal(-2, structure.GetProperty("startBeat").GetSingle());
        Assert.Equal(300, structure.GetProperty("endBeat").GetSingle());
        System.Text.Json.JsonElement signature = Assert.Single(
            structure.GetProperty("signatures").EnumerateArray());
        Assert.Equal(0, signature.GetProperty("marker").GetInt32());
        Assert.Equal(4, signature.GetProperty("beats").GetInt32());
        Assert.Equal(1, structure.GetProperty("previewLoopEnd").GetInt32());

        ClassicJduSongDatabaseService probeService = new(
            mapService.Object,
            Options.Create(new UrlSettings
            {
                HostUrl = "next-service.test",
                JduHostUrl = "jdu-service.test",
                CDNUrl = "cdn.test"
            }),
            Options.Create(new ClassicJduDiagnosticsSettings
            {
                AssetOverrideMapName = "Abracadabra",
                AssetOverrides = new Dictionary<string, string>
                {
                    ["expandCoachImageUrl"] = "https://reference.test/albumcoach.ckd"
                }
            }));
        ClassicJduSongDatabaseEntry probeSong = Assert.Single(
            probeService.CreateDatabase(videoPreviewCodec: null, assetPlatform: "NX")).Value;
        Assert.Equal(
            "https://reference.test/albumcoach.ckd",
            probeSong.Assets["expandCoachImageUrl"]);
    }

    [Fact]
    public void SongDatabaseRoute_SelectsPreviewCodecByClientSkuAndPlatform()
    {
        Mock<IClassicJduSongDatabaseService> service = new();
        service.Setup(value => value.CreateDatabase(It.IsAny<MapVideoCodec?>(), It.IsAny<string>()))
            .Returns(new Dictionary<string, ClassicJduSongDatabaseEntry>());
        ClassicSongDatabaseController controller = new(service.Object);

        controller.GetSongs("jd2017-nx-all");
        controller.GetSongs("jd2018-nx-all");
        controller.GetSongs("jd2018-wiiu-noe");

        service.Verify(value => value.CreateDatabase(null, "NX"), Times.Once);
        service.Verify(value => value.CreateDatabase(MapVideoCodec.Vp9, "NX"), Times.Once);
        service.Verify(value => value.CreateDatabase(MapVideoCodec.Vp8, "WIIU"), Times.Once);
    }

    [Theory]
    [InlineData("Beyoncé", true)]
    [InlineData("François", true)]
    [InlineData("Cafe\u0301", true)]
    [InlineData("ポーカーフェイス", false)]
    [InlineData("蔡依林", false)]
    [InlineData("Танец", false)]
    public void ClassicCatalogText_AllowsLatinDiacriticsButRejectsNonLatinScripts(string value, bool expected)
        => Assert.Equal(expected, JduCatalogSelection.IsClassicCompatibleText(value));

    [Fact]
    public void Carousel_GroupsSongsByTitleRangesAndOriginalVersion()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        AddSong(songs, "Abracadabra", "Alpha", 2017, coachCount: 1);
        AddSong(songs, "PokerFaceALT", "Hotel", 2018, coachCount: 3);
        AddSong(songs, "AQueda", "24K Magic", 2017, coachCount: 2);
        AddSong(songs, "FutureSong", "Future", 2025, coachCount: 4);
        AddSong(songs, "ExcludedMap", "ポーカーフェイス", 2018);
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
        ClassicJduMenuService service = new(mapService.Object);

        JduSongCarouselContentResponse alphabet = service.CreatePage(
            "alphabet",
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));
        JduSongCarouselContentResponse versions = service.CreatePage(
            "originaljdversion",
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));
        JduSongCarouselContentResponse party = service.CreatePage(
            "party",
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));
        JduSongCarouselContentResponse coop = service.CreatePage(
            "partycoop",
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));
        JduSongCarouselContentResponse sweat = service.CreatePage(
            "sweat",
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));

        Assert.Equal(2, alphabet.Categories.Length);
        Assert.Contains(
            alphabet.Categories.Single(category => category.Title == "A-H").Items,
            item => item.Components.Single().MapName == "Abracadabra");
        Assert.Contains(
            alphabet.Categories.Single(category => category.Title == "0-9").Items,
            item => item.Components.Single().MapName == "AQueda");
        Assert.Equal(3, versions.Categories.Length);
        Assert.Contains(versions.Categories, category => category.Title == "Just Dance 2017");
        Assert.Contains(versions.Categories, category => category.Title == "Just Dance 2018");
        Assert.Contains(versions.Categories, category => category.Title == "Just Dance 2025");
        Assert.Equal("Just Dance 2018", party.Categories[0].Title);
        Assert.Equal(
            ["Just Dance 2017", "Double Rumble", "Episodes", "Favorites", "Playlist", "Fitness", "Solo", "Duet", "Trio", "Quatro"],
            party.Categories.Skip(1).Take(10).Select(category => category.Title));
        Assert.Equal("Just Dance 2025", party.Categories[^1].Title);
        JduSongCarouselCategory jd2018 = Assert.Single(
            party.Categories,
            category => category.Title == "Just Dance 2018");
        Assert.Contains(jd2018.Items, item =>
            item.Components.Single().MapName == "Despacito");
        Assert.Contains(jd2018.Items, item =>
            item.Components.Single().MapName == "PokerFaceALT");
        Assert.Equal("play-song", party.ActionLists["partyMap"].Actions[0].Type);
        Assert.Equal("family_rival", party.ActionLists["partyMap"].Actions[0].BannerContext);
        Assert.Equal("song", party.ActionLists["partyMap"].Actions[0].BannerType);
        Assert.DoesNotContain(
            party.Categories.SelectMany(category => category.Items),
            item => item.Components.FirstOrDefault()?.MapName == "ExcludedMap");
        Assert.Contains(
            party.Categories.Single(category => category.Title == "Solo").Items,
            item => item.Components.Single().MapName == "Abracadabra");
        Assert.Contains(
            party.Categories.Single(category => category.Title == "Duet").Items,
            item => item.Components.Single().MapName == "AQueda");
        Assert.Contains(
            party.Categories.Single(category => category.Title == "Trio").Items,
            item => item.Components.Single().MapName == "PokerFaceALT");
        Assert.Contains(
            party.Categories.Single(category => category.Title == "Quatro").Items,
            item => item.Components.Single().MapName == "FutureSong");

        JduSongCarouselCategory episodes = Assert.Single(
            party.Categories,
            category => category.Title == "Episodes");
        Assert.Equal(
            Enumerable.Range(1, 8).Select(episode => $"JDM_Episode{episode}")
                .Append("JD_DMEpisodeRandom"),
            episodes.Items.Select(item => item.Components.Single().Episode));

        JduSongCarouselCategory doubleRumble = Assert.Single(
            party.Categories,
            category => category.Title == "Double Rumble");
        Assert.Equal(
            ["SwitchChef", "SwitchHandyman", "SwitchMariachi", "SwitchSport", "SwitchWitch"],
            doubleRumble.Items.Select(item => item.Components.Single().MapName));
        Assert.All(doubleRumble.Items, item => Assert.Equal("HdRumbleMap", item.ActionList));
        Assert.Equal("hd_rumble", party.ActionLists["HdRumbleMap"].Actions[0].BannerType);
        Assert.All(
            coop.Categories.SelectMany(category => category.Items)
                .Where(item => item.Components.FirstOrDefault()?.MapName is not null),
            item => Assert.Equal("partyMapCoop", item.ActionList));
        Assert.Equal("family_coop", coop.ActionLists["partyMapCoop"].Actions[0].BannerContext);
        Assert.All(
            sweat.Categories.SelectMany(category => category.Items)
                .Where(item => item.Components.FirstOrDefault()?.MapName is not null),
            item => Assert.Equal("sweatMap", item.ActionList));
        Assert.Equal("CONTEXT_SWEAT", sweat.ActionLists["sweatMap"].Actions[0].BannerContext);
    }

    [Fact]
    public void PartyCarousel_PreservesDumpDefinedRowsForEachClassicClient()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        AddSong(songs, "ServerSolo", "Server Solo", 2025, coachCount: 1);
        AddSong(songs, "ServerDuet", "Server Duet", 2025, coachCount: 2);
        AddSong(songs, "ServerTrio", "Server Trio", 2025, coachCount: 3);
        AddSong(songs, "ServerQuatro", "Server Quatro", 2025, coachCount: 4);
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
        ClassicJduMenuService service = new(mapService.Object);

        AssertRows(
            ClassicJduClientProfiles.GetClient(2016, JduPlatform.WiiU),
            ["Just Dance 2016", "SOLO", "DUO", "TRIO", "QUATRO", "SHOWTIME", "MASHUP", "STAR REMIX / COMMUNITY REMIX", "PARTY MASTER"]);
        AssertRows(
            ClassicJduClientProfiles.GetClient(2017, JduPlatform.NintendoSwitch),
            ["Just Dance 2017", "Favorites", "Solo", "Duet", "Trio", "Quatro", "Mashup"]);
        AssertRows(
            ClassicJduClientProfiles.GetClient(2017, JduPlatform.WiiU),
            ["Just Dance 2017", "Favorites", "Solo", "Duet", "Trio", "Quatro", "Mashup"]);
        AssertRows(
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch),
            ["Just Dance 2018", "Double Rumble", "Episodes", "Favorites", "Playlist", "Fitness", "Solo", "Duet", "Trio", "Quatro"]);
        AssertRows(
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.WiiU),
            ["Just Dance 2018", "Episodes", "Favorites", "Playlist", "Fitness", "Solo", "Duet", "Trio", "Quatro"]);
        AssertRows(
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.PlayStation4),
            ["Just Dance 2018", "Episodes", "Favorites", "Playlist", "Fitness", "Solo", "Duet", "Trio", "Quatro"]);

        void AssertRows(ClassicJduClientProfile client, string[] expected)
        {
            JduSongCarouselContentResponse response = service.CreatePage("party", client);
            Assert.Equal(expected, response.Categories.Take(expected.Length).Select(category => category.Title));
        }
    }

    [Fact]
    public void PartyCarousel_WrapsNearestGameYearsAroundCurrentGame()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        AddSong(songs, "Jd1Song", "Old Song", 1);
        AddSong(songs, "Jd2017Song", "Previous Song", 2017);
        AddSong(songs, "Jd2019Song", "Next Song", 2019);
        AddSong(songs, "Jd2025Song", "Future Song", 2025);
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
        ClassicJduMenuService service = new(mapService.Object);

        JduSongCarouselContentResponse response = service.CreatePage(
            "party",
            ClassicJduClientProfiles.GetClient(2018, JduPlatform.NintendoSwitch));

        Assert.Equal("Just Dance 2018", response.Categories[0].Title);
        Assert.Equal("Just Dance 2017", response.Categories[1].Title);
        Assert.Equal("Just Dance 2019", response.Categories[^1].Title);
        Assert.True(
            Array.FindIndex(response.Categories, category => category.Title == "Just Dance 2025")
            < response.Categories.Length - 1);
    }

    [Fact]
    public void PartyCarousel_WrapsAdjacentYearsForJdu2016AndJdu2017()
    {
        foreach ((ClassicJduClientProfile Client, int PreviousYear, int NextYear) testCase in new[]
        {
            (ClassicJduClientProfiles.GetClient(2016, JduPlatform.WiiU), 2015, 2017),
            (ClassicJduClientProfiles.GetClient(2017, JduPlatform.NintendoSwitch), 2016, 2018)
        })
        {
            OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
            AddSong(songs, $"Previous{testCase.PreviousYear}", "Previous Song", testCase.PreviousYear);
            AddSong(songs, $"Next{testCase.NextYear}", "Next Song", testCase.NextYear);
            AddSong(songs, "FarFuture", "Far Future Song", 2025);
            Mock<IMapService> mapService = new();
            mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
            ClassicJduMenuService service = new(mapService.Object);

            JduSongCarouselContentResponse response = service.CreatePage("party", testCase.Client);

            Assert.Equal($"Just Dance {testCase.Client.GameYear}", response.Categories[0].Title);
            Assert.Equal($"Just Dance {testCase.PreviousYear}", response.Categories[1].Title);
            Assert.Equal($"Just Dance {testCase.NextYear}", response.Categories[^1].Title);
        }
    }

    [Fact]
    public void PartyCarousel_TreatsFormerUplaySongsAsServerSongs()
    {
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
        AddSong(songs, "OnMyMind", "On My Mind", 2016);
        AddSong(songs, "ElectroMambo", "Electro Mambo", 2016);
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
        ClassicJduMenuService service = new(mapService.Object);

        JduSongCarouselContentResponse jd2016 = service.CreatePage(
            "party",
            ClassicJduClientProfiles.GetClient(2016, JduPlatform.WiiU));
        JduSongCarouselContentResponse jd2017 = service.CreatePage(
            "party",
            ClassicJduClientProfiles.GetClient(2017, JduPlatform.WiiU));

        Assert.DoesNotContain(jd2016.Categories, category => category.Title == "UPLAY");
        Assert.DoesNotContain(jd2017.Categories, category => category.Title == "Ubisoft Club");
        Assert.Contains(
            jd2016.Categories.Single(category => category.Title == "Just Dance 2016").Items,
            item => item.Components.Single().MapName == "OnMyMind");
        Assert.Contains(
            jd2017.Categories.Single(category => category.Title == "Just Dance 2016").Items,
            item => item.Components.Single().MapName == "ElectroMambo");
    }

    [Fact]
    public void PlatformSpecificRows_MatchPs4AndRetailPartyMasterRules()
    {
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns([]);
        ClassicJduMenuService service = new(mapService.Object);
        ClassicJduClientProfile jd2016Ps4 = ClassicJduClientProfiles.GetClient(2016, JduPlatform.PlayStation4);
        ClassicJduClientProfile jd2016InternalPc = ClassicJduClientProfiles.GetClient(2016, JduPlatform.Pc);
        ClassicJduClientProfile jd2017Ps4 = ClassicJduClientProfiles.GetClient(2017, JduPlatform.PlayStation4);

        Assert.DoesNotContain(service.CreatePage("party", jd2016Ps4).Categories, category =>
            category.Title == "PARTY MASTER");
        Assert.DoesNotContain(service.CreatePage("party", jd2016InternalPc).Categories, category =>
            category.Title == "PARTY MASTER");
        Assert.DoesNotContain(service.CreatePage("party", jd2017Ps4).Categories, category =>
            category.Title == "Ubisoft Club");
        Assert.DoesNotContain(
            service.CreatePage("party", ClassicJduClientProfiles.GetClient(2018, JduPlatform.PlayStation4)).Categories,
            category => category.Title == "Double Rumble");
    }

    [Fact]
    public void DoubleRumble_IsAvailableOnlyOnJdu2018Switch()
    {
        Mock<IMapService> mapService = new();
        mapService.SetupGet(service => service.Maps).Returns([]);
        ClassicJduMenuService service = new(mapService.Object);

        foreach (ClassicJduClientProfile client in ClassicJduClientProfiles.All)
        {
            JduSongCarouselContentResponse party = service.CreatePage("party", client);
            bool hasDoubleRumble = party.Categories.Any(category => category.Title == "Double Rumble");

            Assert.Equal(client.ApplicationId == Guid.Parse("853badc5-e5b8-47a9-be06-630a1071701c"), hasDoubleRumble);
            Assert.Equal(hasDoubleRumble, party.ActionLists.ContainsKey("HdRumbleMap"));
            Assert.Equal(
                client.GameYear == 2018,
                service.CreatePage("kids", client).Categories.Length > 0);
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateClassicJduPlatforms(
        string mapName,
        int coachCount)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> platforms = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (string platform in new[] { "NX", "WIIU" })
        {
            Dictionary<string, string> roles = new(StringComparer.OrdinalIgnoreCase)
            {
                ["BannerBackground"] = Asset("BannerBackground", "banner.ckd"),
                ["AlbumBackground"] = Asset("AlbumBackground", "album.ckd"),
                ["MapBackground"] = Asset("MapBackground", "map.ckd"),
                ["AlbumCoach"] = Asset("AlbumCoach", "albumcoach.ckd"),
                ["CoverSmall"] = Asset("CoverSmall", "cover-small.ckd"),
                ["Cover"] = Asset("Cover", "cover.ckd")
            };
            for (int coach = 1; coach <= coachCount; coach++)
                roles[$"Coach{coach}"] = Asset($"Coach{coach}", $"coach{coach}.ckd");
            platforms[platform] = roles;

            string Asset(string role, string file)
                => $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/{role}/{file}";
        }

        return platforms;
    }

    private static void AddSong(
        OrderedDictionary<Guid, JustDanceSongDBEntry> songs,
        string mapName,
        string title,
        int originalJdVersion,
        int coachCount = 1)
    {
        songs[Guid.NewGuid()] = new JustDanceSongDBEntry
        {
            CoachCount = coachCount,
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Classic")),
            MapName = mapName,
            OriginalJDVersion = originalJdVersion,
            Title = title
        };
    }
}
