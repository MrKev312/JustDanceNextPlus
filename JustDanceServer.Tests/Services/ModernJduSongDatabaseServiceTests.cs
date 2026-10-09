using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Moq;

using System.Text.Json;

using SongDatabaseController = JustDanceServer.Controllers.prod.just_dance.com.songdb.v2.Database;

namespace JustDanceServer.Tests.Services;

public sealed class ModernJduSongDatabaseServiceTests
{
    [Fact]
    public void CreateDatabase_ProjectsSharedJdNextSongsIntoJduShape()
    {
        IModernJduSongDatabaseService service = CreateService(out _);

		IReadOnlyDictionary<string, JduSongDatabaseEntry> database = service.CreateDatabase("NX");
        Assert.Equal(2, database.Count);
		JduSongDatabaseEntry song = database["Abracadabra"];

        Assert.Equal("Abracadabra", song.MapName);
        Assert.Equal("The Artist", song.Artist);
        Assert.Equal("The Title", song.Title);
        Assert.Equal(2025, song.OriginalJDVersion);
        Assert.Equal("Magical Version", song.CustomTypeName);
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
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/test.ogg",
            song.Urls["jmcs://jd-contents/Abracadabra/Abracadabra_AudioPreview.ogg"]);
        Assert.Contains("Abracadabra_AudioPreview.ogg", song.AudioPreviewData);
        Assert.Contains("codecs=\"vp9\"", song.MapPreviewMpd);
        Assert.Contains(
            "jmcs://jd-contents/Abracadabra/Abracadabra_MapPreviewNoSoundCrop_LOW.vp9.webm",
            song.MapPreviewMpd);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/vp9/low.webm",
            song.Urls["jmcs://jd-contents/Abracadabra/Abracadabra_MapPreviewNoSoundCrop_LOW.vp9.webm"]);
    }

    [Fact]
    public void DatabaseController_ReturnsCapturedOctetStreamWithCamelCaseFields()
    {
        IModernJduSongDatabaseService service = CreateService(out _);
        SongDatabaseController controller = new(service);

		FileContentResult result = Assert.IsType<FileContentResult>(controller.GetDatabase("NX"));
        using JsonDocument json = JsonDocument.Parse(result.FileContents);
        JsonElement song = json.RootElement.GetProperty("Abracadabra");

        Assert.Equal("application/octet-stream", result.ContentType);
        Assert.Equal("The Title", song.GetProperty("title").GetString());
        Assert.Equal("Abracadabra_mapContent", song.GetProperty("packages").GetProperty("mapContent").GetString());
    }

    [Fact]
    public void CreateDatabase_UsesVp8PreviewForWiiU()
    {
        IModernJduSongDatabaseService service = CreateService(out _);

		JduSongDatabaseEntry song = service.CreateDatabase("WIIU")["Abracadabra"];

        Assert.Contains("codecs=\"vp8\"", song.MapPreviewMpd);
        Assert.Contains(
            "Abracadabra_MapPreviewNoSoundCrop_LOW.vp8.webm",
            song.MapPreviewMpd);
        Assert.Equal(
            "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/vp8/low.webm",
            song.Urls["jmcs://jd-contents/Abracadabra/Abracadabra_MapPreviewNoSoundCrop_LOW.vp8.webm"]);
    }

    private static IModernJduSongDatabaseService CreateService(out Mock<IMapService> mapService)
    {
		Guid primaryId = Guid.NewGuid();
		JustDanceSongDBEntry primarySong = new()
        {
            Artist = "The Artist",
            AssetsMetadata = new AssetsMetadata
            {
                AudioPreviewTrk = new AudioPreviewTrk
                {
                    Markers = [0, 48_000, 96_000],
                    PreviewEntry = 0
				}
            },
            CoachCount = 2,
            Credits = "The Credits",
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Magical Version")),
            Difficulty = 3,
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
            OriginalJDVersion = 2025,
            SweatDifficulty = 2,
            Tags = ["jdplus"],
            Title = "The Title"
        };
		MapMediaAssets jduMedia = ServerMapTestData.CreateMedia(
			MapMediaLayouts.JDUnlimited,
			CreateJduPlatforms("Abracadabra", coachCount: 2),
			previewAudioUrl: "https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/test.ogg",
			vp8: ServerMapTestData.CreateVideoSet(
				"vp8",
				"https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/vp8",
				[
					new WebmData { Start = 460, End = 950, Duration = 30, Bitrate = 500_000 },
					new WebmData { Start = 461, End = 951, Duration = 30, Bitrate = 1_500_000 }
				],
				["low", "mid"]),
			vp9: ServerMapTestData.CreateVideoSet(
				"vp9",
				"https://cdn.test/maps/Abracadabra/JDUnlimited/Preview/vp9",
				[
					new WebmData { Start = 640, End = 1130, Duration = 30, Bitrate = 500_000 },
					new WebmData { Start = 641, End = 1131, Duration = 30, Bitrate = 1_500_000 }
				],
				["low", "mid"]));

		Guid secondaryId = Guid.NewGuid();
		JustDanceSongDBEntry secondarySong = new()
        {
            DanceVersionLocId = new OasisTag(new LocalizedString(1, "Standard")),
            MapName = "PreviouslyFilteredMap",
            OriginalJDVersion = 2018,
            Title = "Another Title"
        };
		OrderedDictionary<Guid, ServerMap> maps = new()
		{
			[primaryId] = ServerMapTestData.CreateMap(primaryId, Path.Combine("maps", primarySong.MapName), primarySong, jdUnlimited: jduMedia),
			[secondaryId] = ServerMapTestData.CreateMap(secondaryId, Path.Combine("maps", secondarySong.MapName), secondarySong)
		};
        mapService = new Mock<IMapService>();
		mapService.SetupGet(service => service.Maps).Returns(maps);

        JsonSettingsService jsonSettings = new(Mock.Of<IServiceProvider>());
        return new ModernJduSongDatabaseService(
            mapService.Object,
            jsonSettings,
            Options.Create(new JustDanceServer.Configuration.UrlSettings
            {
                HostUrl = "next-service.test",
                JduHostUrl = "jdu-service.test",
                CDNUrl = "cdn.test"
            }), Mock.Of<ILocalizedStringService>());
    }

	private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateJduPlatforms(
		string mapName,
		int coachCount)
	{
		Dictionary<string, IReadOnlyDictionary<string, string>> platforms =
			[with(StringComparer.OrdinalIgnoreCase)];
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
}
