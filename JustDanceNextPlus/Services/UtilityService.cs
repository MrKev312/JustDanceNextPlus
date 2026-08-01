using AssetsTools.NET;
using AssetsTools.NET.Extra;

using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface IUtilityService
{
	ValueTask<LocalJustDanceSongDBEntry> LoadMapDBEntryAsync(string mapFolder, string platform = ClientPlatform.DefaultPlatform);
    IReadOnlyDictionary<string, AssetMetadata> LoadAssetMetadata(string mapFolder, string platform = ClientPlatform.DefaultPlatform);
	ContentAuthorization LoadContentAuthorization(string mapFolder, string platform = ClientPlatform.DefaultPlatform);
	WebmData[] GetVideoData(string videoFolder);
	string GetAssetUrl(string mapFolder, string name, string platform = ClientPlatform.DefaultPlatform);
	string? TryGetAssetUrl(string mapFolder, string name, string platform = ClientPlatform.DefaultPlatform);
	(string low, string mid, string high, string ultra) GetVideoUrls(WebmData[] videoData, string mapFolder);
	(string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) GetAssetUrls(string mapFolder, string platform = ClientPlatform.DefaultPlatform);
	SongDBAssets LoadSongDBAssets(string mapFolder, WebmData[] videoData, string platform = ClientPlatform.DefaultPlatform);
	AssetsAuthorization LoadContentAuthorizationAssets(string mapFolder, WebmData[] videoData, string platform = ClientPlatform.DefaultPlatform);
	(string low, string mid, string high, string ultra) GetContentAuthorizationVideoUrls(WebmData[] videoData, string mapFolder);
	AudioPreviewTrk GenerateAudioPreviewTrk(string mapPackagePath);
	bool HasCameraScoring(string mapPackagePath);
}

public class UtilityService(JsonSettingsService jsonSettingsService,
	IOptions<UrlSettings> urlOptions,
	ILogger<UtilityService> logger,
	IFileSystem fileSystem,
	IWebmExtractor webmExtractor) : IUtilityService
{
	private readonly UrlSettings urlSettings = urlOptions.Value;

	public async ValueTask<LocalJustDanceSongDBEntry> LoadMapDBEntryAsync(string mapFolder, string platform = ClientPlatform.DefaultPlatform)
	{
		try
		{
			string songInfoPath = Path.Combine(mapFolder, "SongInfo.json");
			if (!fileSystem.FileExists(songInfoPath))
				throw new FileNotFoundException($"Missing SongInfo.json in {mapFolder}");

			using Stream fs = fileSystem.OpenRead(songInfoPath);
			LocalJustDanceSongDBEntry songInfo = (await JsonSerializer.DeserializeAsync<LocalJustDanceSongDBEntry>(fs, jsonSettingsService.PrettyPascalFormat))
				?? throw new InvalidOperationException($"Failed to deserialize SongInfo.json in {mapFolder}");

			songInfo = NormalizeSongInfoForBoot(songInfo, mapFolder, logger);

			if (string.IsNullOrEmpty(songInfo.MapName))
				throw new InvalidOperationException($"MapName is null or empty in {songInfoPath}");

			// If the mapId is null or empty, throw an exception
			if (string.IsNullOrEmpty(songInfo.MapName))
				throw new InvalidOperationException($"MapName is null or empty in {songInfoPath}");

			WebmData[] videoData = GetVideoData(Path.Combine(mapFolder, "videoPreview"));

			// Update songInfo with asset URLs and metadata
			(string mapPackageFolder, _) = ResolveAssetFolder(mapFolder, "MapPackage", platform);
			string[] mapPackageFiles = fileSystem.GetFiles(mapPackageFolder);
			if (mapPackageFiles.Length == 0)
				throw new FileNotFoundException($"Missing map package in {mapPackageFolder}");

			string mapPackagePath = mapPackageFiles[0];
			AudioPreviewTrk audioPreviewTrk;
			bool hasCameraScoring;
			using (MapPackageSession mapPackageSession = LoadMapPackageSession(mapPackagePath))
			{
				audioPreviewTrk = GenerateAudioPreviewTrk(mapPackageSession.Manager, mapPackageSession.AssetsFileInstance);
				hasCameraScoring = HasCameraScoring(mapPackageSession.Manager, mapPackageSession.AssetsFileInstance);
			}

			songInfo = songInfo with
			{
				HasCameraScoring = hasCameraScoring,
				AssetsMetadata = new AssetsMetadata
				{
					VideoData = [.. videoData],
					AudioPreviewTrk = audioPreviewTrk
				},
				Assets = LoadSongDBAssets(mapFolder, videoData, platform)
			};

			// If there's no game version tag, add a "jdplus" tag
			if (!songInfo.Tags.Any(tag => tag.Equals("jdplus", StringComparison.OrdinalIgnoreCase)
				|| tag.StartsWith("songpack_", StringComparison.OrdinalIgnoreCase)
				|| tag.StartsWith("Music_Pack_", StringComparison.OrdinalIgnoreCase)))
				songInfo = songInfo with { Tags = songInfo.Tags.Add("jdplus") };

			return songInfo;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to load map DB entry: ({mapFolder})", mapFolder);
			throw new InvalidOperationException($"Failed to load map DB entry: ({mapFolder})", ex);
		}
	}

	public (string low, string mid, string high, string ultra) GetVideoUrls(WebmData[] videoData, string mapFolder)
	{
		if (videoData.Length == 0)
			throw new InvalidOperationException("No video data provided");

		string[] urls = new string[4];
		string baseUrl = $"{CdnBaseUrl}/maps/{Path.GetFileName(mapFolder)}/videoPreview";

		for (int i = 0; i < 4; i++)
		{
			urls[i] = i < videoData.Length
				? $"{baseUrl}/{videoData[i].FileName}.webm"
				: urls[i - 1];
		}

		return (urls[0], urls[1], urls[2], urls[3]);
	}

	public (string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) GetAssetUrls(string mapFolder, string platform = ClientPlatform.DefaultPlatform)
	{
		// Compute values, preferring existing values
		string audioPreview = GetAssetUrl(mapFolder, "audioPreview_opus", platform);
		string coachesLarge = GetAssetUrl(mapFolder, "coachesLarge", platform);
		string coachesSmall = GetAssetUrl(mapFolder, "coachesSmall", platform);
		string cover = GetAssetUrl(mapFolder, "cover", platform);
		string? songTitleLogo = TryGetAssetUrl(mapFolder, "songTitleLogo", platform);

		return (audioPreview, coachesLarge, coachesSmall, cover, songTitleLogo);
	}

	public SongDBAssets LoadSongDBAssets(string mapFolder, WebmData[] videoData, string platform = ClientPlatform.DefaultPlatform)
	{
		(string low, string mid, string high, string ultra) = GetVideoUrls(videoData, mapFolder);
		(string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) = GetAssetUrls(mapFolder, platform);

		return new SongDBAssets
		{
			AudioPreview_opus = audioPreview,
			VideoPreview_HIGH_vp8_webm = high,
			VideoPreview_HIGH_vp9_webm = high,
			VideoPreview_LOW_vp8_webm = low,
			VideoPreview_LOW_vp9_webm = low,
			VideoPreview_MID_vp8_webm = mid,
			VideoPreview_MID_vp9_webm = mid,
			VideoPreview_ULTRA_vp8_webm = ultra,
			VideoPreview_ULTRA_vp9_webm = ultra,
			CoachesLarge = coachesLarge,
			CoachesSmall = coachesSmall,
			Cover = cover,
			SongTitleLogo = songTitleLogo
		};
	}

	public IReadOnlyDictionary<string, AssetMetadata> LoadAssetMetadata(string mapFolder, string platform = ClientPlatform.DefaultPlatform)
	{
        ImmutableDictionary<string, AssetMetadata>.Builder assetMetadata = ImmutableDictionary.CreateBuilder<string, AssetMetadata>();
        mapFolder = Path.GetFullPath(mapFolder);
		string normalizedPlatform = ClientPlatform.Normalize(platform);

		// This now gets strings, not FileInfo objects
		string[] allFiles = fileSystem.GetFiles(mapFolder, "*", SearchOption.AllDirectories);

		void AddAsset(string relativeFolder, string? key = null)
		{
			key ??= relativeFolder;

			IEnumerable<string> matchingFiles = allFiles
			 .Where(f => f.Contains(Path.Combine(mapFolder, relativeFolder), StringComparison.OrdinalIgnoreCase));

			// Use the abstraction to get the length for sorting
			string largestFile = matchingFiles
			 .OrderByDescending(fileSystem.GetFileLength)
			 .First();

			string hash = Path.GetFileNameWithoutExtension(largestFile).ToLowerInvariant();
			assetMetadata[key] = new AssetMetadata(hash, fileSystem.GetFileLength(largestFile));
		}

		AddAsset(Path.Combine(normalizedPlatform, "Cover"), "cover");
		AddAsset("AudioPreview_opus", "audioPreview.opus");
		AddAsset("Audio_opus", "audio.opus");
		AddAsset(Path.Combine(normalizedPlatform, "CoachesSmall"), "coachesSmall");
		AddAsset(Path.Combine(normalizedPlatform, "CoachesLarge"), "coachesLarge");
		AddAsset(Path.Combine(normalizedPlatform, "MapPackage"), "mapPackage");

		void AddVideoAsset(string subfolder, string[] keys, Func<IReadOnlyList<string>, string?> selector)
		{
			List<string> matchingFiles = [.. allFiles
			.Where(f => Path.GetDirectoryName(f)!
			.EndsWith(Path.Combine(mapFolder, subfolder), StringComparison.OrdinalIgnoreCase))];

			string? file = selector(matchingFiles);
			if (file != null)
			{
				string hash = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
				long length = fileSystem.GetFileLength(file);
				foreach (string key in keys)
				{
					assetMetadata[key] = new AssetMetadata(hash, length);
				}
			}
		}

		string? GetNthOrLast(IReadOnlyList<string> list, int index) =>
		 list.Count == 0 ? null : list[Math.Min(index, list.Count - 1)];

		// videoPreview: second smallest or fallback
		AddVideoAsset("videoPreview",
		["videoPreview_MID.vp8.webm", "videoPreview_MID.vp9.webm"],
		files => GetNthOrLast([.. files.OrderBy(f => fileSystem.GetFileLength(f))], 1));

		// video: third smallest or fallback
		AddVideoAsset("video",
		["video_HIGH.hd.webm", "video_HIGH.vp9.webm"],
		files => GetNthOrLast([.. files.OrderBy(f => fileSystem.GetFileLength(f))], 2));

		// video: largest
		AddVideoAsset("video",
		["video_ULTRA.hd.webm"],
		files => files.OrderBy(f => fileSystem.GetFileLength(f)).LastOrDefault());

		return assetMetadata.ToImmutable();
    }

	public ContentAuthorization LoadContentAuthorization(string mapFolder, string platform = ClientPlatform.DefaultPlatform)
	{
		WebmData[] videoData = GetVideoData(Path.Combine(mapFolder, "video"));

		ContentAuthorization contentAuthorization = new()
		{
			Assets = LoadContentAuthorizationAssets(mapFolder, videoData, platform),
			AssetsMetadata = new AssetsmetadataAuthorization()
			{
				VideoData = [.. videoData]
			}
		};

		return contentAuthorization;
	}

	public AssetsAuthorization LoadContentAuthorizationAssets(string mapFolder, WebmData[] videoData, string platform = ClientPlatform.DefaultPlatform)
	{
		(string low, string mid, string high, string ultra) = GetContentAuthorizationVideoUrls(videoData, mapFolder);

		return new AssetsAuthorization
		{
			AudioOpus = GetAssetUrl(mapFolder, "Audio_opus", platform)!,
			VideoLowHdWebm = low,
			VideoLowVP9Webm = low,
			VideoMidHdWebm = mid,
			VideoMidVP9Webm = mid,
			VideoHighHdWebm = high,
			VideoHighVP9Webm = high,
			VideoUltraHdWebm = ultra,
			VideoUltraVP9Webm = ultra,
			MapPackage = GetAssetUrl(mapFolder, "MapPackage", platform)!
		};
	}

	public (string low, string mid, string high, string ultra) GetContentAuthorizationVideoUrls(
		WebmData[] videoData,
		string mapFolder)
	{
		if (videoData.Length == 0)
			throw new InvalidOperationException("No video data provided");

		string baseUrl = $"{CdnBaseUrl}/maps/{Path.GetFileName(mapFolder)}/video";
		string[] urls = new string[4];

		for (int i = 0; i < 4; i++)
		{
			urls[i] = i < videoData.Length
				? $"{baseUrl}/{videoData[i].FileName}.webm"
				: urls[i - 1];
		}

		return (urls[0], urls[1], urls[2], urls[3]);
	}

	public WebmData[] GetVideoData(string videoFolder)
	{
		string[] videoFiles = fileSystem.GetFiles(videoFolder, "*.webm");

		if (videoFiles.Length == 0)
			throw new FileNotFoundException($"Missing video file in {videoFolder}");

		if (videoFiles.Length > 4)
			throw new InvalidOperationException($"Too many video files in {videoFolder}");

		WebmData[] webmData = new WebmData[videoFiles.Length];

		Parallel.For(0, videoFiles.Length, i =>
		{
			WebmData data = webmExtractor.GetCuesInfo(videoFiles[i]);
			long fileLength = fileSystem.GetFileLength(videoFiles[i]);

			if (CuesAppearAtEnd(data, fileLength))
			{
				logger.LogWarning(
					"WebM file '{VideoFile}' has cues at the end ({CueStart}-{CueEnd} of {FileLength} bytes). Run mkclean or regenerate with front-loaded cues.",
					videoFiles[i],
					data.Start,
					data.End,
					fileLength);
			}

			webmData[i] = data;
		});

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

	internal static LocalJustDanceSongDBEntry NormalizeSongInfoForBoot(LocalJustDanceSongDBEntry songInfo, string mapFolder, ILogger logger)
	{
		if (songInfo.SweatDifficulty != 0)
			return songInfo;

		logger.LogWarning(
			"Map '{MapName}' in '{MapFolder}' has sweatDifficulty 0; defaulting to 1.",
			string.IsNullOrWhiteSpace(songInfo.MapName) ? Path.GetFileName(mapFolder) : songInfo.MapName,
			mapFolder);

		return songInfo with { SweatDifficulty = 1 };
	}

	internal static bool CuesAppearAtEnd(WebmData data, long fileLength)
	{
		const long TailToleranceBytes = 4096;

		return fileLength > TailToleranceBytes
			&& data.Start > TailToleranceBytes
			&& data.End >= fileLength - TailToleranceBytes;
	}

	public string GetAssetUrl(string mapFolder, string name, string platform = ClientPlatform.DefaultPlatform)
	{
		(string assetFolder, string urlFolder) = ResolveAssetFolder(mapFolder, name, platform);

		if (fileSystem.DirectoryExists(assetFolder))
		{
			string[] files = fileSystem.GetFiles(assetFolder);
			if (files.Length == 0)
				throw new FileNotFoundException($"Missing asset file in {assetFolder}");

			return $"{CdnBaseUrl}/maps/{Path.GetFileName(mapFolder)}/{urlFolder}/{Path.GetFileName(files[0])}";
		}

		throw new DirectoryNotFoundException($"Missing asset folder {assetFolder}");
	}

	public string? TryGetAssetUrl(string mapFolder, string name, string platform = ClientPlatform.DefaultPlatform)
	{
		(string assetFolder, string urlFolder) = ResolveAssetFolder(mapFolder, name, platform);

		if (fileSystem.DirectoryExists(assetFolder))
		{
			string[] files = fileSystem.GetFiles(assetFolder);
			if (files.Length == 0)
				return null;

			return $"{CdnBaseUrl}/maps/{Path.GetFileName(mapFolder)}/{urlFolder}/{Path.GetFileName(files[0])}";
		}

		return null;
	}

	private string CdnBaseUrl
	{
		get
		{
			string trimmedUrl = urlSettings.CDNUrl.Trim().TrimEnd('/');
			return trimmedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				|| trimmedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
				? trimmedUrl
				: $"https://{trimmedUrl}";
		}
	}

	private static (string AssetFolder, string UrlFolder) ResolveAssetFolder(string mapFolder, string name, string platform)
	{
		string canonicalName = CanonicalAssetFolderName(name);
		if (IsPlatformSpecificAssetFolder(canonicalName))
		{
			string normalizedPlatform = ClientPlatform.Normalize(platform);
			return (
				Path.Combine(mapFolder, normalizedPlatform, canonicalName),
				$"{normalizedPlatform}/{canonicalName}");
		}

		return (Path.Combine(mapFolder, canonicalName), canonicalName);
	}

	private static string CanonicalAssetFolderName(string name)
	{
		return name.Trim().ToLowerInvariant() switch
		{
			"audio_opus" or "audio" => "Audio_opus",
			"audiopreview_opus" or "audiopreview" => "AudioPreview_opus",
			"coacheslarge" => "CoachesLarge",
			"coachessmall" => "CoachesSmall",
			"cover" => "Cover",
			"mappackage" => "MapPackage",
			"songtitlelogo" => "songTitleLogo",
			_ => name
		};
	}

	private static bool IsPlatformSpecificAssetFolder(string name)
	{
		return name is "CoachesLarge" or "CoachesSmall" or "Cover" or "MapPackage" or "songTitleLogo";
	}

	public static string GenerateMpd(IEnumerable<WebmData> webms, bool isPreview = false)
	{
		if (!webms.Any())
			return "";
		if (webms.Count() > 4)
			throw new InvalidOperationException("Too many video files");

		string baseUrlPrefix = isPreview ? "videoPreview_" : "video_";
		int maxWidth = isPreview ? 768 : 1920;
		int maxHeight = isPreview ? 432 : 1080;
		string durationFormatted = webms.ElementAt(0).Duration.ToString(CultureInfo.InvariantCulture);
		int bufferTime = 10;

		StringBuilder sb = new();

		sb.Append($"""
			<?xml version="1.0"?>
			<MPD xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="urn:mpeg:DASH:schema:MPD:2011" xsi:schemaLocation="urn:mpeg:DASH:schema:MPD:2011" type="static" mediaPresentationDuration="PT{durationFormatted}S" minBufferTime="PT{bufferTime}S" profiles="urn:webm:dash:profile:webm-on-demand:2012">
			<Period id="0" start="PT0S" duration="PT{durationFormatted}S">
			<AdaptationSet id="0" mimeType="video/webm" codecs="vp8,vp9" lang="eng" maxWidth="{maxWidth}" maxHeight="{maxHeight}" subsegmentAlignment="true" subsegmentStartsWithSAP="1" bitstreamSwitching="true">

			""");

		string[] qualities = ["LOW", "MID", "HIGH", "ULTRA"];
		for (int i = 0; i < webms.Count(); i++)
		{
			WebmData webm = webms.ElementAt(i);

			sb.Append($"""
				<Representation id="{i}" bandwidth="{webm.Bitrate}">
				<BaseURL>{baseUrlPrefix}{qualities[i % 4]}.vp9.webm</BaseURL>
				<SegmentBase indexRange="{webm.Start}-{webm.End}">
				<Initialization range="0-{webm.Start}" />
				</SegmentBase>
				</Representation>

				""");
		}

		sb.Append("""
			</AdaptationSet>
			</Period>
			</MPD>

			""");

		return sb.ToString();
	}

	public AudioPreviewTrk GenerateAudioPreviewTrk(string mapPackagePath)
	{
		if (!fileSystem.FileExists(mapPackagePath))
			throw new FileNotFoundException($"Missing map package in {mapPackagePath}");

		using MapPackageSession mapPackageSession = LoadMapPackageSession(mapPackagePath);
		return GenerateAudioPreviewTrk(mapPackageSession.Manager, mapPackageSession.AssetsFileInstance);
	}

	public AudioPreviewTrk GenerateAudioPreviewTrk(AssetsManager manager, AssetsFileInstance assetsFileInstance)
	{
		int musicTrackScriptIndex = FindScriptIndex(
			manager,
			assetsFileInstance,
			className => className == "MusicTrack",
			"Failed to find MusicTrack script info");

		List<AssetFileInfo> infos = assetsFileInstance.file.GetAssetsOfType((int)AssetClassID.MonoBehaviour, (ushort)musicTrackScriptIndex);
		AssetFileInfo musicTrackInfo = infos.FirstOrDefault()
			?? throw new InvalidOperationException("Failed to find MusicTrack MonoBehaviour");
		AssetTypeValueField trackInfo = manager.GetBaseField(assetsFileInstance, musicTrackInfo);
		AssetTypeValueField structure = trackInfo["m_structure"]["MusicTrackStructure"];

		return new AudioPreviewTrk
		{
			StartBeat = structure["startBeat"].AsFloat,
			EndBeat = structure["endBeat"].AsFloat,
			PreviewDuration = structure.Children.FirstOrDefault(x => x.FieldName == "previewDuration")?.AsFloat ?? 30,
			Markers = [.. structure["markers"]["Array"].Children.Select(x => x["VAL"].AsInt)],
			VideoStartTime = structure["videoStartTime"].AsFloat,
			PreviewLoopStart = structure["previewLoopStart"].AsFloat,
			PreviewEntry = structure["previewEntry"].AsFloat
		};
	}

	public bool HasCameraScoring(string mapPackagePath)
	{
		if (!fileSystem.FileExists(mapPackagePath))
			throw new FileNotFoundException($"Missing map package in {mapPackagePath}");

		using MapPackageSession mapPackageSession = LoadMapPackageSession(mapPackagePath);
		return HasCameraScoring(mapPackageSession.Manager, mapPackageSession.AssetsFileInstance);
	}

	public bool HasCameraScoring(AssetsManager manager, AssetsFileInstance assetsFileInstance)
	{
		List<AssetFileInfo> textAssetInfos = assetsFileInstance.file.GetAssetsOfType((int)AssetClassID.TextAsset);
		foreach (AssetFileInfo textAssetInfo in textAssetInfos)
		{
			AssetTypeValueField textAsset = manager.GetBaseField(assetsFileInstance, textAssetInfo);
			string? assetName = textAsset["m_Name"].AsString;
			if (assetName != null && assetName.EndsWith(".gesture", StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static int FindScriptIndex(
		AssetsManager manager,
		AssetsFileInstance assetsFileInstance,
		Func<string, bool> predicate,
		string notFoundMessage)
	{
		foreach (KeyValuePair<int, AssetTypeReference> fileInfo in AssetHelper.GetAssetsFileScriptInfos(manager, assetsFileInstance))
		{
			if (predicate(fileInfo.Value.ClassName))
				return fileInfo.Key;
		}

		throw new InvalidOperationException(notFoundMessage);
	}

	private MapPackageSession LoadMapPackageSession(string mapPackagePath)
	{
		AssetsManager manager = new();
		BundleFileInstance bundleFileInstance = manager.LoadBundleFile(mapPackagePath, true);
		AssetsFileInstance assetsFileInstance = manager.LoadAssetsFileFromBundle(bundleFileInstance, 0, false);
		return new MapPackageSession(manager, assetsFileInstance);
	}

	private sealed class MapPackageSession(AssetsManager manager, AssetsFileInstance assetsFileInstance) : IDisposable
	{
		public AssetsManager Manager { get; } = manager;
		public AssetsFileInstance AssetsFileInstance { get; } = assetsFileInstance;

		public void Dispose()
		{
			Manager.UnloadAll(true);
		}
	}

}
