using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Extensions.Options;

using Moq;

namespace JustDanceServer.Tests.Services;

public sealed class JduContentAuthorizationServiceTests
{
	[Fact]
	public void ContentAuthorization_UsesClientVideoCodecAndWiiUQualityPolicy()
	{
		const string mapName = "PokerFaceALT";
		string mapFolder = Path.Combine("maps", mapName);
		Guid songId = Guid.NewGuid();
		OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
		songs[songId] = new JustDanceSongDBEntry
		{
			DanceVersionLocId = new OasisTag(new LocalizedString(1, "Magical Version")),
			MapName = mapName
		};

		Mock<IMapService> mapService = new();
		MapMediaAssets jduMedia = ServerMapTestData.CreateMedia(
			MapMediaLayouts.JDUnlimited,
			new Dictionary<string, IReadOnlyDictionary<string, string>>(),
			fullAudioUrl: "https://cdn.test/maps/PokerFaceALT/JDUnlimited/Audio/audiohash.ogg");
		MapVideoSet vp8 = ServerMapTestData.CreateVideoSet(
			"vp8",
			"https://cdn.test/maps/PokerFaceALT/Video/vp8",
			[
				new WebmData { FileName = "lowhash", Bitrate = 700_000 },
				new WebmData { FileName = "midhash", Bitrate = 2_000_000 },
				new WebmData { FileName = "highhash", Bitrate = 4_000_000 },
				new WebmData { FileName = "ultrahash", Bitrate = 8_000_000 }
			],
			["lowhash", "midhash", "highhash", "ultrahash"]);
		ServerMap map = ServerMapTestData.CreateMap(
			songId,
			mapFolder,
			songs[songId],
			jdUnlimited: jduMedia,
			fullVideos: new Dictionary<string, MapVideoSet>(StringComparer.OrdinalIgnoreCase)
			{
				["vp8"] = vp8,
				["vp9"] = ServerMapTestData.CreateVideoSet(
					"vp9",
					"https://cdn.test/maps/PokerFaceALT/Video/vp9",
					[
						new WebmData { FileName = "vp9low", Bitrate = 600_000 },
						new WebmData { FileName = "vp9mid", Bitrate = 1_800_000 },
						new WebmData { FileName = "vp9high", Bitrate = 3_500_000 },
						new WebmData { FileName = "vp9ultra", Bitrate = 7_000_000 }
					],
					["vp9low", "vp9mid", "vp9high", "vp9ultra"])
			});
		mapService.Setup(service => service.GetMap(mapName)).Returns(map);

		JduContentAuthorizationService service = new(
			mapService.Object,
			Options.Create(new UrlSettings
			{
				HostUrl = "next-service.test",
				CDNUrl = "cdn.test",
				JduHostUrl = "jdu-service.test"
			}));

		JduContentAuthorizationEntry wiiU = Assert.IsType<JduContentAuthorizationEntry>(
			service.Create(mapName, ResolveMedia("jd2018-wiiu-noe")));
		Assert.Equal(9, wiiU.Urls.Count);
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/JDUnlimited/Audio/audiohash.ogg",
			wiiU.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT.ogg"]);
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp8/lowhash.webm",
			wiiU.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_LOW.webm"]);
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp8/highhash.webm",
			wiiU.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_HIGH.webm"]);
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp8/midhash.webm",
			wiiU.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_MID.hd.webm"]);
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp8/highhash.webm",
			wiiU.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_ULTRA.hd.webm"]);

		JduContentAuthorizationEntry switch2017 = Assert.IsType<JduContentAuthorizationEntry>(
			service.Create(mapName, ResolveMedia("jd2017-nx-all")));
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp8/ultrahash.webm",
			switch2017.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_ULTRA.hd.webm"]);

		JduContentAuthorizationEntry switch2018 = Assert.IsType<JduContentAuthorizationEntry>(
			service.Create(mapName, ResolveMedia("jd2018-nx-all")));
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp9/vp9low.webm",
			switch2018.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_LOW.webm"]);
		Assert.Equal(
			"https://cdn.test/maps/PokerFaceALT/Video/vp9/vp9ultra.webm",
			switch2018.Urls["jmcs://jd-contents/PokerFaceALT/PokerFaceALT_ULTRA.hd.webm"]);
	}

	private static JduMediaClientProfile ResolveMedia(string skuId)
	{
		Assert.True(JduMediaClientProfiles.TryResolve(skuId, out JduMediaClientProfile? profile));
		return profile;
	}
}
