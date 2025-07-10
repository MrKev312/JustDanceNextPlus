using AssetsTools.NET;
using AssetsTools.NET.Extra;

using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Globalization;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class UtilityService(JsonSettingsService jsonSettingsService, IOptions<UrlSettings> urlOptions, ILogger<UtilityService> logger)
{
	private readonly UrlSettings urlSettings = urlOptions.Value;

	public async ValueTask<LocalJustDanceSongDBEntry> LoadMapDBEntryAsync(string mapFolder)
	{
		try
		{
			string songInfoPath = Path.Combine(mapFolder, "SongInfo.json");
			if (!File.Exists(songInfoPath))
				throw new FileNotFoundException($"Missing SongInfo.json in {mapFolder}");

			using FileStream fs = File.OpenRead(songInfoPath);
			LocalJustDanceSongDBEntry songInfo = (await JsonSerializer.DeserializeAsync<LocalJustDanceSongDBEntry>(fs, jsonSettingsService.PrettyPascalFormat))
				?? throw new InvalidOperationException($"Failed to deserialize SongInfo.json in {mapFolder}");

			songInfo.Assets = new();

			// If the mapId is null or empty, throw an exception
			if (string.IsNullOrEmpty(songInfo.MapName))
				throw new InvalidOperationException($"MapName is null or empty in {songInfoPath}");

			WebmData[] videoData = GetVideoUrls(Path.Combine(mapFolder, "videoPreview"));
			AssignVideoUrls(songInfo, videoData, mapFolder);
			songInfo.AssetsMetadata.VideoData = videoData;
			songInfo.AssetsMetadata.AudioPreviewTrk ??= GenerateAudioPreviewTrk(Directory.GetFiles(Path.Combine(mapFolder, "MapPackage"))[0]);

			songInfo.Assets.AudioPreview_opus ??= GetAssetUrl(mapFolder, "audioPreview_opus");
			songInfo.Assets.CoachesLarge ??= GetAssetUrl(mapFolder, "coachesLarge");
			songInfo.Assets.CoachesSmall ??= GetAssetUrl(mapFolder, "coachesSmall");
			songInfo.Assets.Cover ??= GetAssetUrl(mapFolder, "cover");
			songInfo.Assets.SongTitleLogo ??= GetAssetUrl(mapFolder, "songTitleLogo", true);

			AssignAssetUrls(songInfo, mapFolder);

			return songInfo;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to load map DB entry: ({mapFolder})", mapFolder);
			throw new InvalidOperationException($"Failed to load map DB entry: ({mapFolder})", ex);
		}
	}

	public void AssignVideoUrls(LocalJustDanceSongDBEntry songInfo, WebmData[] videoData, string mapFolder)
	{
		for (int i = 0; i < videoData.Length; i++)
		{
			string url = $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/videoPreview/{videoData[i].FileName}.webm";
			switch (i)
			{
				case 0:
					songInfo.Assets.VideoPreview_LOW_vp9_webm = url;
					break;
				case 1:
					songInfo.Assets.VideoPreview_MID_vp9_webm = url;
					break;
				case 2:
					songInfo.Assets.VideoPreview_HIGH_vp9_webm = url;
					break;
				case 3:
					songInfo.Assets.VideoPreview_ULTRA_vp9_webm = url;
					break;
			}
		}
	}

	public void AssignAssetUrls(LocalJustDanceSongDBEntry songInfo, string mapFolder)
	{
		songInfo.Assets.AudioPreview_opus ??= GetAssetUrl(mapFolder, "audioPreview_opus");
	}

	public Dictionary<string, AssetMetadata> LoadAssetMetadata(string mapFolder)
	{
		var assetMetadata = new Dictionary<string, AssetMetadata>();
		mapFolder = Path.GetFullPath(mapFolder);

		var allFiles = new DirectoryInfo(mapFolder)
			.GetFiles("*", SearchOption.AllDirectories);

		void AddAsset(string relativeFolder, string? key = null)
		{
			key ??= relativeFolder;

			var matchingFiles = allFiles
				.Where(f => f.FullName.Contains(Path.Combine(mapFolder, relativeFolder), StringComparison.OrdinalIgnoreCase));

			var largestFile = matchingFiles
				.OrderByDescending(f => f.Length)
				.First();

			var hash = Path.GetFileNameWithoutExtension(largestFile.Name).ToLowerInvariant();
			assetMetadata[key] = new AssetMetadata(hash, largestFile.Length);
		}

		AddAsset("cover", "cover");
		AddAsset("audioPreview_opus", "audioPreview.opus");
		AddAsset("audio", "audio.opus");
		AddAsset("coachesSmall");
		AddAsset("coachesLarge");
		AddAsset("MapPackage", "mapPackage");

		void AddVideoAsset(string subfolder, string[] keys, Func<IReadOnlyList<FileInfo>, FileInfo?> selector)
		{
			var matchingFiles = allFiles
				.Where(f => Path.GetDirectoryName(f.FullName)!
					.EndsWith(Path.Combine(mapFolder, subfolder), StringComparison.OrdinalIgnoreCase))
				.ToList();

			var file = selector(matchingFiles);
			if (file != null)
			{
				var hash = Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant();
				foreach (var key in keys)
				{
					assetMetadata[key] = new AssetMetadata(hash, file.Length);
				}
			}
		}

		FileInfo? GetNthOrLast(IReadOnlyList<FileInfo> list, int index) =>
			list.Count == 0 ? null : list[Math.Min(index, list.Count - 1)];

		// videoPreview: second smallest or fallback
		AddVideoAsset("videoPreview",
			["videoPreview_MID.vp8.webm", "videoPreview_MID.vp9.webm"],
			files => GetNthOrLast([.. files.OrderBy(f => f.Length)], 1));

		// video: third smallest or fallback
		AddVideoAsset("video",
			["video_HIGH.hd.webm", "video_HIGH.vp9.webm"],
			files => GetNthOrLast([.. files.OrderBy(f => f.Length)], 2));

		// video: largest
		AddVideoAsset("video",
			["video_ULTRA.hd.webm"],
			files => files.OrderBy(f => f.Length).LastOrDefault());

		return assetMetadata;
	}

	public ContentAuthorization LoadContentAuthorization(string mapFolder)
	{
		ContentAuthorization contentAuthorization = new()
		{
			Assets = new AssetsAuthorization
			{
				AudioOpus = GetAssetUrl(mapFolder, "Audio_opus")!,
				MapPackage = GetAssetUrl(mapFolder, "MapPackage")!
			}
		};

		WebmData[] videoData = GetVideoUrls(Path.Combine(mapFolder, "video"));
		contentAuthorization.AssetsMetadata.VideoData = videoData;

		AssignContentAuthorizationVideoUrls(contentAuthorization, videoData, mapFolder);

		return contentAuthorization;
	}

	public void AssignContentAuthorizationVideoUrls(ContentAuthorization contentAuthorization, WebmData[] videoData, string mapFolder)
	{
		for (int i = 0; i < videoData.Length; i++)
		{
			string url = $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/video/{videoData[i].FileName}.webm";
			switch (i)
			{
				case 0:
					contentAuthorization.Assets.VideoLowVP9Webm = url;
					break;
				case 1:
					contentAuthorization.Assets.VideoMidVP9Webm = url;
					break;
				case 2:
					contentAuthorization.Assets.VideoHighVP9Webm = url;
					break;
				case 3:
					contentAuthorization.Assets.VideoUltraVP9Webm = url;
					break;
			}
		}
	}

	public static WebmData[] GetVideoUrls(string videoFolder)
	{
		string[] videoFiles = Directory.GetFiles(videoFolder, "*.webm");

		if (videoFiles.Length == 0)
			throw new FileNotFoundException($"Missing video file in {videoFolder}");

		if (videoFiles.Length > 4)
			throw new InvalidOperationException($"Too many video files in {videoFolder}");

		WebmData[] webmData = new WebmData[videoFiles.Length];

		Parallel.For(0, videoFiles.Length, i => webmData[i] = WebmCuesExtractor.GetCuesInfo(videoFiles[i]));

		// Sort by bitrate
		Array.Sort(webmData, (a, b) => a.Bitrate.CompareTo(b.Bitrate));

		// Duplicate the last one until there are 4
		while (webmData.Length < 4)
		{
			WebmData lastWebm = webmData[^1];
			Array.Resize(ref webmData, webmData.Length + 1);
			webmData[^1] = new WebmData
			{
				FileName = lastWebm.FileName,
				Start = lastWebm.Start,
				End = lastWebm.End,
				Duration = lastWebm.Duration,
				Bitrate = lastWebm.Bitrate
			};
		}

		return webmData;
	}

	public string? GetAssetUrl(string mapFolder, string name, bool canBeMissing = false)
	{
		string assetFolder = Path.Combine(mapFolder, name);

		if (Directory.Exists(assetFolder))
		{
			string[] files = Directory.GetFiles(assetFolder);
			if (files.Length == 0)
				throw new FileNotFoundException($"Missing asset file in {assetFolder}");

			return $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/{name}/{Path.GetFileName(files[0])}";
		}

		// If it's a songTitleLogo, that's fine cuz it's optional
		if (canBeMissing)
			return null;

		throw new DirectoryNotFoundException($"Missing asset folder {assetFolder}");
	}

	public static string GenerateMpd(WebmData[] webms, bool isPreview = false)
	{
		if (webms.Length == 0)
			throw new InvalidOperationException("No video files found");
		if (webms.Length > 4)
			throw new InvalidOperationException("Too many video files");

		string baseUrlPrefix = isPreview ? "videoPreview_" : "video_";
		int maxWidth = isPreview ? 768 : 1920;
		int maxHeight = isPreview ? 432 : 1080;
		string durationFormatted = webms[0].Duration.ToString(CultureInfo.InvariantCulture);
		int bufferTime = 10;

		string response = $"""
			<?xml version="1.0"?>
			<MPD xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="urn:mpeg:DASH:schema:MPD:2011" xsi:schemaLocation="urn:mpeg:DASH:schema:MPD:2011" type="static" mediaPresentationDuration="PT{durationFormatted}S" minBufferTime="PT{bufferTime}S" profiles="urn:webm:dash:profile:webm-on-demand:2012">
				<Period id="0" start="PT0S" duration="PT{durationFormatted}S">
					<AdaptationSet id="0" mimeType="video/webm" codecs="vp8,vp9" lang="eng" maxWidth="{maxWidth}" maxHeight="{maxHeight}" subsegmentAlignment="true" subsegmentStartsWithSAP="1" bitstreamSwitching="true">

			""";

		string[] qualities = ["LOW", "MID", "HIGH", "ULTRA"];
		for (int i = 0; i < webms.Length; i++)
		{
			response += $"""
				<Representation id="{i}" bandwidth="{webms[i].Bitrate}">
				<BaseURL>{baseUrlPrefix}{qualities[i % 4]}.vp9.webm</BaseURL>
					<SegmentBase indexRange="{webms[i].Start}-{webms[i].End}">
						<Initialization range="0-{webms[i].Start}" />
					</SegmentBase>
				</Representation>

			""";
		}

		response += """
					</AdaptationSet>
				</Period>
			</MPD>

			""";

		return response;
	}

	public string GenerateAudioPreviewTrk(string mapPackagePath)
	{
		if (!File.Exists(mapPackagePath))
			throw new FileNotFoundException($"Missing map package in {mapPackagePath}");

		// Load map package
		AssetsManager manager = new();
		BundleFileInstance bunInst = manager.LoadBundleFile(mapPackagePath, true);
		AssetBundleFile bun = bunInst.file;
		AssetsFileInstance afileInst = manager.LoadAssetsFileFromBundle(bunInst, 0, false);
		AssetsFile afile = afileInst.file;
		afile.GenerateQuickLookup();

		// Grab the MonoBehaviour
		AssetFileInfo? musicTrackInfo = afile.AssetInfos
			.FirstOrDefault(x => x.TypeId == (int)AssetClassID.MonoBehaviour &&
				manager.GetBaseField(afileInst, x)["m_Name"].AsString == "")
			?? throw new InvalidOperationException("Failed to find MusicTrack MonoBehaviour");

		AssetTypeValueField trackInfo = manager.GetBaseField(afileInst, musicTrackInfo);

		AssetTypeValueField structure = trackInfo["m_structure"]["MusicTrackStructure"];

		// Create an AudioPreviewTrk object
		AudioPreviewTrk audioPreviewTrk = new()
		{
			StartBeat = structure["startBeat"].AsFloat,
			EndBeat = structure["endBeat"].AsFloat,
			PreviewDuration = structure.Children.FirstOrDefault(x => x.FieldName == "previewDuration")?.AsFloat ?? 30,
			Markers = [.. structure["markers"]["Array"].Children.Select(x => x["VAL"].AsInt)],
			VideoStartTime = structure["videoStartTime"].AsFloat,
			PreviewLoopStart = structure["previewLoopStart"].AsFloat,
			PreviewEntry = structure["previewEntry"].AsFloat
		};

		// Serialize the object
		return JsonSerializer.Serialize(audioPreviewTrk, jsonSettingsService.ShortFormat);
	}
}