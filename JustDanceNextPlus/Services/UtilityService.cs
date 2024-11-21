﻿using AssetsTools.NET;
using AssetsTools.NET.Extra;

using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public class UtilityService(JsonSettingsService jsonSettingsService)
{
	public async Task<LocalJustDanceSongDBEntry> LoadMapDBEntryAsync(string mapFolder)
	{
		string songInfoPath = Path.Combine(mapFolder, "SongInfo.json");
		if (!File.Exists(songInfoPath))
			throw new FileNotFoundException($"Missing SongInfo.json in {mapFolder}");

		string songInfoJson = await File.ReadAllTextAsync(songInfoPath);
		LocalJustDanceSongDBEntry songInfo = JsonSerializer.Deserialize<LocalJustDanceSongDBEntry>(songInfoJson, jsonSettingsService.PrettyPascalFormat)
			?? throw new JsonException("Failed to deserialize SongInfo.json");

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

	public static void AssignVideoUrls(LocalJustDanceSongDBEntry songInfo, WebmData[] videoData, string mapFolder)
	{
		for (int i = 0; i < videoData.Length; i++)
		{
			string url = $"https://prod-next.just-dance.com/maps/{Path.GetFileName(mapFolder)}/videoPreview/{videoData[i].FileName}.webm";
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

	public static void AssignAssetUrls(LocalJustDanceSongDBEntry songInfo, string mapFolder)
	{
		songInfo.Assets.AudioPreview_opus ??= GetAssetUrl(mapFolder, "audioPreview_opus");
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

	public static void AssignContentAuthorizationVideoUrls(ContentAuthorization contentAuthorization, WebmData[] videoData, string mapFolder)
	{
		for (int i = 0; i < videoData.Length; i++)
		{
			string url = $"https://prod-next.just-dance.com/maps/{Path.GetFileName(mapFolder)}/video/{videoData[i].FileName}.webm";
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

		return webmData;
	}

	public static string? GetAssetUrl(string mapFolder, string name, bool canBeMissing = false)
	{
		string assetFolder = Path.Combine(mapFolder, name);

		if (Directory.Exists(assetFolder))
		{
			string[] files = Directory.GetFiles(assetFolder);
			if (files.Length == 0)
				throw new FileNotFoundException($"Missing asset file in {assetFolder}");

			return $"https://prod-next.just-dance.com/maps/{Path.GetFileName(mapFolder)}/{name}/{Path.GetFileName(files[0])}";
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
			Markers = structure["markers"]["Array"].Children.Select(x => x["VAL"].AsInt).ToArray(),
			VideoStartTime = structure["videoStartTime"].AsFloat,
			PreviewLoopStart = structure["previewLoopStart"].AsFloat,
			PreviewEntry = structure["previewEntry"].AsFloat
		};

		// Serialize the object
		return JsonSerializer.Serialize(audioPreviewTrk, jsonSettingsService.ShortFormat);
	}
}