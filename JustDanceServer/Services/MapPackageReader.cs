using AssetsTools.NET;
using AssetsTools.NET.Extra;

using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Utilities;

namespace JustDanceServer.Services;

public interface IMapPackageReader
{
	MapPackageMetadata ReadMetadata(string mapPackagePath);
}

public sealed record MapPackageMetadata(AudioPreviewTrk AudioPreviewTrack, bool HasCameraScoring);

public sealed class MapPackageReader(IFileSystem fileSystem) : IMapPackageReader
{
	public MapPackageMetadata ReadMetadata(string mapPackagePath)
	{
		using MapPackageSession session = Open(mapPackagePath);
		return new MapPackageMetadata(
			ReadAudioPreviewTrack(session),
			HasCameraScoring(session));
	}

	private static AudioPreviewTrk ReadAudioPreviewTrack(MapPackageSession session)
	{
		int musicTrackScriptIndex = FindScriptIndex(
			session.Manager,
			session.AssetsFile,
			className => className == "MusicTrack",
			"Failed to find MusicTrack script info");

		List<AssetFileInfo> infos = session.AssetsFile.file.GetAssetsOfType(
			(int)AssetClassID.MonoBehaviour,
			(ushort)musicTrackScriptIndex);
		AssetFileInfo musicTrackInfo = infos.FirstOrDefault()
			?? throw new InvalidOperationException("Failed to find MusicTrack MonoBehaviour");
		AssetTypeValueField trackInfo = session.Manager.GetBaseField(session.AssetsFile, musicTrackInfo);
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

	private static bool HasCameraScoring(MapPackageSession session)
	{
		List<AssetFileInfo> textAssets = session.AssetsFile.file.GetAssetsOfType((int)AssetClassID.TextAsset);
		foreach (AssetFileInfo textAssetInfo in textAssets)
		{
			AssetTypeValueField textAsset = session.Manager.GetBaseField(session.AssetsFile, textAssetInfo);
			string? assetName = textAsset["m_Name"].AsString;
			if (assetName?.EndsWith(".gesture", StringComparison.OrdinalIgnoreCase) == true)
				return true;
		}

		return false;
	}

	private MapPackageSession Open(string mapPackagePath)
	{
		if (!fileSystem.FileExists(mapPackagePath))
			throw new FileNotFoundException($"Missing map package in {mapPackagePath}");

		AssetsManager manager = new();
		BundleFileInstance bundle = manager.LoadBundleFile(mapPackagePath, true);
		AssetsFileInstance assetsFile = manager.LoadAssetsFileFromBundle(bundle, 0, false);
		return new MapPackageSession(manager, assetsFile);
	}

	private static int FindScriptIndex(
		AssetsManager manager,
		AssetsFileInstance assetsFile,
		Func<string, bool> predicate,
		string notFoundMessage)
	{
		foreach (KeyValuePair<int, AssetTypeReference> script in AssetHelper.GetAssetsFileScriptInfos(manager, assetsFile))
		{
			if (predicate(script.Value.ClassName))
				return script.Key;
		}

		throw new InvalidOperationException(notFoundMessage);
	}

	private sealed class MapPackageSession(AssetsManager manager, AssetsFileInstance assetsFile) : IDisposable
	{
		public AssetsManager Manager { get; } = manager;
		public AssetsFileInstance AssetsFile { get; } = assetsFile;

		public void Dispose() => Manager.UnloadAll(true);
	}
}
