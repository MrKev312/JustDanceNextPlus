using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Utilities;

using System.Collections.Immutable;

namespace JustDanceServer.Services;

public interface IJdPlusAssetMetadataLoader
{
	IReadOnlyDictionary<string, AssetMetadata> Load(
		string mapFolder,
		string platform,
		MapMediaLayout previewLayout);
}

public sealed class JdPlusAssetMetadataLoader(IFileSystem fileSystem) : IJdPlusAssetMetadataLoader
{
	public IReadOnlyDictionary<string, AssetMetadata> Load(
		string mapFolder,
		string platform,
		MapMediaLayout previewLayout)
	{
		ImmutableDictionary<string, AssetMetadata>.Builder metadata =
			ImmutableDictionary.CreateBuilder<string, AssetMetadata>();
		mapFolder = Path.GetFullPath(mapFolder);
		string normalizedPlatform = ClientPlatform.Normalize(platform);
		string[] allFiles = fileSystem.GetFiles(mapFolder, "*", SearchOption.AllDirectories);

		void AddAsset(string relativeFolder, string? key = null, string? extension = null)
		{
			key ??= relativeFolder;
			string assetFolder = Path.Combine(mapFolder, relativeFolder);
			string largestFile = allFiles
				.Where(file => Path.GetDirectoryName(file)!.Equals(assetFolder, StringComparison.OrdinalIgnoreCase)
					&& (extension is null || Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase)))
				.OrderByDescending(fileSystem.GetFileLength)
				.First();

			metadata[key] = CreateMetadata(largestFile);
		}

		string jdPlusPlatformAssets = Path.Combine("JDPlus", "Assets", normalizedPlatform);
		AddAsset(Path.Combine(jdPlusPlatformAssets, "Cover"), "cover");
		AddAsset(
			MapMediaLayouts.JDPlus.PreviewAudio.RelativeFolder.Replace('/', Path.DirectorySeparatorChar),
			"audioPreview.opus",
			MapMediaLayouts.JDPlus.PreviewAudio.FileExtension);
		AddAsset(
			MapMediaLayouts.JDPlus.FullAudio.RelativeFolder.Replace('/', Path.DirectorySeparatorChar),
			"audio.opus",
			MapMediaLayouts.JDPlus.FullAudio.FileExtension);
		AddAsset(Path.Combine(jdPlusPlatformAssets, "CoachesSmall"), "coachesSmall");
		AddAsset(Path.Combine(jdPlusPlatformAssets, "CoachesLarge"), "coachesLarge");
		AddAsset(Path.Combine(jdPlusPlatformAssets, "MapPackage"), "mapPackage");

		AddVideoAsset(
			previewLayout.GetPreviewVideoFolder("vp8").Replace('/', Path.DirectorySeparatorChar),
			["videoPreview_MID.vp8.webm"],
			files => GetNthOrLast(files, 1));
		AddVideoAsset(
			previewLayout.GetPreviewVideoFolder("vp9").Replace('/', Path.DirectorySeparatorChar),
			["videoPreview_MID.vp9.webm"],
			files => GetNthOrLast(files, 1));
		AddVideoAsset(
			Path.Combine("Video", "vp8"),
			["video_HIGH.hd.webm"],
			files => GetNthOrLast(files, 2));
		AddVideoAsset(
			Path.Combine("Video", "vp9"),
			["video_HIGH.vp9.webm"],
			files => GetNthOrLast(files, 2));
		AddVideoAsset(
			Path.Combine("Video", "vp8"),
			["video_ULTRA.hd.webm"],
			files => files.LastOrDefault());

		return metadata.ToImmutable();

		void AddVideoAsset(
			string subfolder,
			IReadOnlyList<string> keys,
			Func<IReadOnlyList<string>, string?> selector)
		{
			List<string> files = [.. allFiles
				.Where(file => Path.GetDirectoryName(file)!.Equals(
					Path.Combine(mapFolder, subfolder),
					StringComparison.OrdinalIgnoreCase)
					&& Path.GetExtension(file).Equals(".webm", StringComparison.OrdinalIgnoreCase))
				.OrderBy(fileSystem.GetFileLength)];
			string? selectedFile = selector(files);
			if (selectedFile is null)
				return;

			AssetMetadata asset = CreateMetadata(selectedFile);
			foreach (string key in keys)
				metadata[key] = asset;
		}

		AssetMetadata CreateMetadata(string file)
			=> new(
				Path.GetFileNameWithoutExtension(file).ToLowerInvariant(),
				fileSystem.GetFileLength(file));
	}

	private static string? GetNthOrLast(IReadOnlyList<string> files, int index)
		=> files.Count == 0 ? null : files[Math.Min(index, files.Count - 1)];
}
