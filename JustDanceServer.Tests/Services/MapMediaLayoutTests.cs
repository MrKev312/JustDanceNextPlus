using JustDanceServer.Services;
using JustDanceServer.Utilities;

namespace JustDanceServer.Tests.Services;

public class MapMediaLayoutTests
{
    [Theory]
    [InlineData("JDPlus", "JDPlus/Preview", ".opus", "JDPlus/Audio", ".opus")]
    [InlineData("JDUnlimited", "JDUnlimited/Preview", ".ogg", "JDUnlimited/Audio", ".ogg")]
    public void RegisteredLayouts_DeclareTheirOwnAudioLocations(
        string folderName,
        string previewFolder,
        string previewExtension,
        string fullFolder,
        string fullExtension)
    {
        MapMediaLayout layout = Assert.Single(
            MapMediaLayouts.All,
            candidate => candidate.FolderName == folderName);

        Assert.Equal(previewFolder, layout.PreviewAudio.RelativeFolder);
        Assert.Equal(previewExtension, layout.PreviewAudio.FileExtension);
        Assert.Equal(fullFolder, layout.FullAudio.RelativeFolder);
        Assert.Equal(fullExtension, layout.FullAudio.FileExtension);
    }

    [Fact]
    public void AudioLayout_CanDescribeDifferentPreviewAndFullFormats()
    {
        MapMediaLayout layout = new(
            "FutureSubscription",
            new MapAudioLayout("FutureSubscription/Preview", ".preview"),
            new MapAudioLayout("FutureSubscription/Audio", ".full"),
            ["future-codec"]);

        Assert.Equal(".preview", layout.PreviewAudio.FileExtension);
        Assert.Equal(".full", layout.FullAudio.FileExtension);
    }

    [Fact]
    public void VideoSet_KeepsMetadataAndUrlTogetherInQualityOrder()
    {
        MapVideoSet videos = new(
            MapVideoCodec.Vp9,
            [
                new(new WebmData { Bitrate = 100 }, "low"),
                new(new WebmData { Bitrate = 200 }, "mid"),
                new(new WebmData { Bitrate = 300 }, "high"),
                new(new WebmData { Bitrate = 400 }, "ultra")
            ]);

        Assert.Equal(["low", "mid", "high", "ultra"], videos.Qualities.Select(quality => quality.Url));
    }

    [Fact]
    public void VideoSet_RejectsQualitiesThatAreNotSortedByBitrate()
    {
        Assert.Throws<ArgumentException>(() => new MapVideoSet(
            MapVideoCodec.Vp9,
            [
                new(new WebmData { Bitrate = 200 }, "wrong-low"),
                new(new WebmData { Bitrate = 100 }, "wrong-mid"),
                new(new WebmData { Bitrate = 300 }, "high"),
                new(new WebmData { Bitrate = 400 }, "ultra")
            ]));
    }
}
