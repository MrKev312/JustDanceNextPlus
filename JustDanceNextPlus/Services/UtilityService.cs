using AssetsTools.NET;
using AssetsTools.NET.Extra;

using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Utilities;

using Microsoft.Extensions.Options;

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace JustDanceNextPlus.Services;

public interface IUtilityService
{
	ValueTask<LocalJustDanceSongDBEntry> LoadMapDBEntryAsync(string mapFolder);
	Dictionary<string, AssetMetadata> LoadAssetMetadata(string mapFolder);
	ContentAuthorization LoadContentAuthorization(string mapFolder);
	WebmData[] GetVideoData(string videoFolder);
	string GetAssetUrl(string mapFolder, string name);
	string? TryGetAssetUrl(string mapFolder, string name);
	(string low, string mid, string high, string ultra) GetVideoUrls(WebmData[] videoData, string mapFolder);
	(string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) GetAssetUrls(string mapFolder);
	(string low, string mid, string high, string ultra) GetContentAuthorizationVideoUrls(WebmData[] videoData, string mapFolder);
	AudioPreviewTrk GenerateAudioPreviewTrk(string mapPackagePath);
}

public class UtilityService(JsonSettingsService jsonSettingsService,
	IOptions<UrlSettings> urlOptions,
	ILogger<UtilityService> logger,
	IFileSystem fileSystem,
	IWebmExtractor webmExtractor) : IUtilityService
{
	private readonly UrlSettings urlSettings = urlOptions.Value;

	public async ValueTask<LocalJustDanceSongDBEntry> LoadMapDBEntryAsync(string mapFolder)
	{
		try
		{
			string songInfoPath = Path.Combine(mapFolder, "SongInfo.json");
			if (!fileSystem.FileExists(songInfoPath))
				throw new FileNotFoundException($"Missing SongInfo.json in {mapFolder}");

			using Stream fs = fileSystem.OpenRead(songInfoPath);
			LocalJustDanceSongDBEntry songInfo = (await JsonSerializer.DeserializeAsync<LocalJustDanceSongDBEntry>(fs, jsonSettingsService.PrettyPascalFormat))
				?? throw new InvalidOperationException($"Failed to deserialize SongInfo.json in {mapFolder}");

			if (string.IsNullOrEmpty(songInfo.MapName))
				throw new InvalidOperationException($"MapName is null or empty in {songInfoPath}");

			// If the mapId is null or empty, throw an exception
			if (string.IsNullOrEmpty(songInfo.MapName))
				throw new InvalidOperationException($"MapName is null or empty in {songInfoPath}");

			WebmData[] videoData = GetVideoData(Path.Combine(mapFolder, "videoPreview"));

			// Compute asset URLs
			(string low, string mid, string high, string ultra) = GetVideoUrls(videoData, mapFolder);
			(string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) = GetAssetUrls(mapFolder);

			// Update songInfo with asset URLs and metadata
			songInfo = songInfo with
			{
				AssetsMetadata = new AssetsMetadata
				{
					VideoData = [.. videoData],
					AudioPreviewTrk = GenerateAudioPreviewTrk(fileSystem.GetFiles(Path.Combine(mapFolder, "MapPackage"))[0])
				},
				Assets = new SongDBAssets
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
				}
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
		string baseUrl = $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/videoPreview";

		for (int i = 0; i < 4; i++)
		{
			urls[i] = i < videoData.Length
				? $"{baseUrl}/{videoData[i].FileName}.webm"
				: urls[i - 1];
		}

		return (urls[0], urls[1], urls[2], urls[3]);
	}

	public (string audioPreview, string coachesLarge, string coachesSmall, string cover, string? songTitleLogo) GetAssetUrls(string mapFolder)
	{
		// Compute values, preferring existing values
		string audioPreview = GetAssetUrl(mapFolder, "audioPreview_opus");
		string coachesLarge = GetAssetUrl(mapFolder, "coachesLarge");
		string coachesSmall = GetAssetUrl(mapFolder, "coachesSmall");
		string cover = GetAssetUrl(mapFolder, "cover");
		string? songTitleLogo = TryGetAssetUrl(mapFolder, "songTitleLogo");

		return (audioPreview, coachesLarge, coachesSmall, cover, songTitleLogo);
	}

	public Dictionary<string, AssetMetadata> LoadAssetMetadata(string mapFolder)
	{
		Dictionary<string, AssetMetadata> assetMetadata = [];
		mapFolder = Path.GetFullPath(mapFolder);

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

		AddAsset("cover", "cover");
		AddAsset("audioPreview_opus", "audioPreview.opus");
		AddAsset("audio", "audio.opus");
		AddAsset("coachesSmall");
		AddAsset("coachesLarge");
		AddAsset("MapPackage", "mapPackage");

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

		return assetMetadata;
	}

	public ContentAuthorization LoadContentAuthorization(string mapFolder)
	{
		WebmData[] videoData = GetVideoData(Path.Combine(mapFolder, "video"));

		(string? low, string? mid, string? high, string? ultra) = GetContentAuthorizationVideoUrls(videoData, mapFolder);

		ContentAuthorization contentAuthorization = new()
		{
			Assets = new AssetsAuthorization
			{
				AudioOpus = GetAssetUrl(mapFolder, "Audio_opus")!,
				VideoLowHdWebm = low,
				VideoLowVP9Webm = low,
				VideoMidHdWebm = mid,
				VideoMidVP9Webm = mid,
				VideoHighHdWebm = high,
				VideoHighVP9Webm = high,
				VideoUltraHdWebm = ultra,
				VideoUltraVP9Webm = ultra,
				MapPackage = GetAssetUrl(mapFolder, "MapPackage")!
			},
			AssetsMetadata = new AssetsmetadataAuthorization()
			{
				VideoData = [.. videoData]
			}
		};

		return contentAuthorization;
	}

	public (string low, string mid, string high, string ultra) GetContentAuthorizationVideoUrls(
		WebmData[] videoData,
		string mapFolder)
	{
		if (videoData.Length == 0)
			throw new InvalidOperationException("No video data provided");

		string baseUrl = $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/video";
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

		Parallel.For(0, videoFiles.Length, i => webmData[i] = webmExtractor.GetCuesInfo(videoFiles[i]));

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

	public string GetAssetUrl(string mapFolder, string name)
	{
		string assetFolder = Path.Combine(mapFolder, name);

		if (fileSystem.DirectoryExists(assetFolder))
		{
			string[] files = fileSystem.GetFiles(assetFolder);
			if (files.Length == 0)
				throw new FileNotFoundException($"Missing asset file in {assetFolder}");

			return $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/{name}/{Path.GetFileName(files[0])}";
		}

		throw new DirectoryNotFoundException($"Missing asset folder {assetFolder}");
	}

	public string? TryGetAssetUrl(string mapFolder, string name)
	{
		string assetFolder = Path.Combine(mapFolder, name);

		if (fileSystem.DirectoryExists(assetFolder))
		{
			string[] files = fileSystem.GetFiles(assetFolder);
			if (files.Length == 0)
				return null;

			return $"https://{urlSettings.CDNUrl}/maps/{Path.GetFileName(mapFolder)}/{name}/{Path.GetFileName(files[0])}";
		}

		return null;
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

		// Load map package
		AssetsManager manager = new();
		BundleFileInstance bunInst = manager.LoadBundleFile(mapPackagePath, true);
		AssetBundleFile bun = bunInst.file;
		AssetsFileInstance afileInst = manager.LoadAssetsFileFromBundle(bunInst, 0, false);
		AssetsFile afile = afileInst.file;

        // Find the MusicTrack index
        int? index = null;
        foreach (var fileInfo in AssetHelper.GetAssetsFileScriptInfos(manager, afileInst))
        {
            if (fileInfo.Value.ClassName != "MusicTrack")
                continue;

            index = fileInfo.Key;
        }

        if (index == null)
            throw new InvalidOperationException("Failed to find MusicTrack script info");

        // Grab the MonoBehaviour
        var infos = afile.GetAssetsOfType((int)AssetClassID.MonoBehaviour, (ushort)index.Value);
        AssetFileInfo? musicTrackInfo = infos.ElementAtOrDefault(index.Value)
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

		manager.UnloadAll(true);

		// Serialize the object
		return audioPreviewTrk;
	}
}
