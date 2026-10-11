using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

namespace JustDanceServer.Tests.Services;

internal static class ServerMapTestData
{
	public static OrderedDictionary<Guid, ServerMap> ToServerMaps(
		this IEnumerable<KeyValuePair<Guid, JustDanceSongDBEntry>> songs)
	{
		OrderedDictionary<Guid, ServerMap> maps = [];
		foreach ((Guid songId, JustDanceSongDBEntry song) in songs)
			maps[songId] = CreateMap(songId, Path.Combine("maps", song.MapName), song);

		return maps;
	}

	public static ServerMap CreateMap(
		Guid songId,
		string folderPath,
		JustDanceSongDBEntry song,
		MapMediaAssets? jdPlus = null,
		MapMediaAssets? jdUnlimited = null,
		IReadOnlyDictionary<string, MapVideoSet>? fullVideos = null)
	{
		jdPlus ??= CreateMedia(MapMediaLayouts.JDPlus, CreateJdPlusPlatforms(song.MapName));
		jdUnlimited ??= CreateMedia(MapMediaLayouts.JDUnlimited, CreateJduPlatforms(song.MapName));
		fullVideos ??= new Dictionary<string, MapVideoSet>(StringComparer.OrdinalIgnoreCase)
		{
			["vp8"] = CreateVideoSet("vp8", $"https://cdn.test/maps/{song.MapName}/Video/vp8"),
			["vp9"] = CreateVideoSet("vp9", $"https://cdn.test/maps/{song.MapName}/Video/vp9")
		};

		return new ServerMap(
			songId,
			folderPath,
			song,
			new Dictionary<string, MapMediaAssets>(StringComparer.OrdinalIgnoreCase)
			{
				[jdPlus.Layout.FolderName] = jdPlus,
				[jdUnlimited.Layout.FolderName] = jdUnlimited
			},
			fullVideos);
	}

	public static MapMediaAssets CreateMedia(
		MapMediaLayout layout,
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> platformAssets,
		string? previewAudioUrl = null,
		string? fullAudioUrl = null,
		MapVideoSet? vp8 = null,
		MapVideoSet? vp9 = null)
		=> new(
			layout,
			previewAudioUrl ?? $"https://cdn.test/{layout.FolderName}/preview{layout.PreviewAudio.FileExtension}",
			fullAudioUrl ?? $"https://cdn.test/{layout.FolderName}/audio{layout.FullAudio.FileExtension}",
			new Dictionary<string, MapVideoSet>(StringComparer.OrdinalIgnoreCase)
			{
				["vp8"] = vp8 ?? CreateVideoSet("vp8", $"https://cdn.test/{layout.FolderName}/Preview/vp8"),
				["vp9"] = vp9 ?? CreateVideoSet("vp9", $"https://cdn.test/{layout.FolderName}/Preview/vp9")
			},
			platformAssets);

	public static MapVideoSet CreateVideoSet(
		string codec,
		string baseUrl,
		IReadOnlyList<WebmData>? sourceQualities = null,
		IReadOnlyList<string>? sourceNames = null)
	{
		List<WebmData> qualities = sourceQualities is null
			? [new() { Bitrate = 1 }, new() { Bitrate = 2 }, new() { Bitrate = 3 }, new() { Bitrate = 4 }]
			: [.. sourceQualities];
		List<string> names = sourceNames is null
			? ["low", "mid", "high", "ultra"]
			: [.. sourceNames];

		while (qualities.Count < MapVideoSet.QualityCount)
			qualities.Add(qualities[^1]);
		while (names.Count < MapVideoSet.QualityCount)
			names.Add(names[^1]);

		return new MapVideoSet(
			MapVideoCodecExtensions.ParseCodecName(codec),
			[.. qualities.Select((data, index) => new MapVideoQuality(
				data,
				$"{baseUrl}/{names[index]}.webm"))]);
	}

	private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateJdPlusPlatforms(string mapName)
		=> new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
		{
			[ClientPlatform.NintendoSwitch] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["CoachesLarge"] = $"https://cdn.test/maps/{mapName}/JDPlus/Assets/NX/CoachesLarge/asset",
				["CoachesSmall"] = $"https://cdn.test/maps/{mapName}/JDPlus/Assets/NX/CoachesSmall/asset",
				["Cover"] = $"https://cdn.test/maps/{mapName}/JDPlus/Assets/NX/Cover/asset",
				["MapPackage"] = $"https://cdn.test/maps/{mapName}/JDPlus/Assets/NX/MapPackage/asset"
			}
		};

	private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CreateJduPlatforms(string mapName)
	{
		Dictionary<string, IReadOnlyDictionary<string, string>> platforms =
			new(StringComparer.OrdinalIgnoreCase);
		foreach (string platform in new[] { "NX", "WIIU" })
		{
			platforms[platform] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["BannerBackground"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/BannerBackground/asset.ckd",
				["AlbumBackground"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/AlbumBackground/asset.ckd",
				["MapBackground"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/MapBackground/asset.ckd",
				["AlbumCoach"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/AlbumCoach/asset.ckd",
				["CoverSmall"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/CoverSmall/asset.ckd",
				["Cover"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/Cover/asset.ckd",
				["Coach1"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/Coach1/asset.ckd",
				["Coach2"] = $"https://cdn.test/maps/{mapName}/JDUnlimited/Assets/{platform}/Coach2/asset.ckd"
			};
		}

		return platforms;
	}
}
