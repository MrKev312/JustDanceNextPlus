using JustDanceServer.Services;
using JustDanceServer.Utilities;

using System.Xml.Linq;

namespace JustDanceServer.Tests.Services;

public sealed class DashManifestWriterTests
{
    private const string DashNamespace = "urn:mpeg:DASH:schema:MPD:2011";

    [Theory]
    [InlineData(MapVideoCodec.Vp8, 1, 12)]
    [InlineData(MapVideoCodec.Vp9, 8, 12)]
    [InlineData(MapVideoCodec.Vp9, 8, 6)]
    [InlineData(MapVideoCodec.Vp8, 8, 1)]
    public void VideoSet_ProjectsAnyPositiveCountWithoutDiscardingSources(
        MapVideoCodec codec,
        int sourceCount,
        int slotCount)
    {
        MapVideoQuality[] files = [.. Enumerable.Range(1, sourceCount).Select(index =>
            new MapVideoQuality(
                new WebmData { Bitrate = index * 1000 },
                $"https://cdn.test/{index}.webm"))];

        MapVideoSet source = new(codec, files);
        MapVideoSet projected = source.WithQualityCount(slotCount);

        Assert.Equal(sourceCount, source.Qualities.Count);
        Assert.Equal(slotCount, projected.Qualities.Count);
        Assert.Same(files[^1], projected.Qualities[^1]);
        Assert.Equal(files, projected.WithQualityCount(sourceCount).Qualities);

        if (slotCount > sourceCount)
            Assert.All(projected.Qualities.Skip(sourceCount), quality => Assert.Same(files[^1], quality));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void VideoSet_RejectsNonPositiveSlotCounts(int count)
    {
        MapVideoSet source = new(MapVideoCodec.Vp9,
            [new MapVideoQuality(new WebmData(), "https://cdn.test/video.webm")]);

        Assert.Throws<ArgumentOutOfRangeException>(() => source.WithQualityCount(count));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GeneratedManifest_UsesProtocolExactDashNamespace(bool preview)
    {
        WebmData[] videos =
        [
            new()
            {
                Start = 609,
                End = 1099,
                Duration = 30,
                Bitrate = 647291
            }
        ];

        string manifest = preview
            ? DashManifestWriter.GeneratePreview(videos)
            : DashManifestWriter.GenerateFullVideo(new MapVideoSet(
                MapVideoCodec.Vp9,
                [.. Enumerable.Repeat(videos[0], VideoQualityLayout.Standard.Count)
                    .Select(video => new MapVideoQuality(video, "https://cdn.test/video.webm"))]));
        XDocument document = XDocument.Parse(manifest);

        Assert.Equal(DashNamespace, document.Root!.Name.NamespaceName);
        Assert.Equal(
            DashNamespace,
            document.Root.GetDefaultNamespace().NamespaceName);
        Assert.Equal(
            DashNamespace,
            document.Root.Attribute(XName.Get("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"))?
                .Value);
    }

    [Theory]
    [InlineData("vp8", "hd")]
    [InlineData("vp9", "vp9")]
    public void FullManifest_UsesTheVideoSetsActualCodecAndAssetSuffix(string codec, string assetSuffix)
    {
        MapVideoSet videos = ServerMapTestData.CreateVideoSet(codec, "https://cdn.test/video");

        string manifest = DashManifestWriter.GenerateFullVideo(videos);

        Assert.Contains($"codecs=\"{codec}\"", manifest);
        Assert.Contains($"video_HIGH.{assetSuffix}.webm", manifest);
        Assert.Equal(4, XDocument.Parse(manifest).Descendants(XName.Get("Representation", DashNamespace)).Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void FullVp9Manifest_PadsMissingSlotsWithoutChangingExistingEntries(int fileCount)
    {
        WebmData[] source = [.. Enumerable.Range(1, fileCount).Select(index => new WebmData
        {
            Duration = 194.04,
            Bitrate = index * 1_000_000,
            Start = 800 + index,
            End = 4200 + index
        })];
        MapVideoSet videos = new(MapVideoCodec.Vp9,
            [.. source.Select((data, index) => new MapVideoQuality(data, $"https://cdn.test/{index}.webm"))]);
        XDocument legacy = XDocument.Parse(DashManifestWriter.GenerateFullVideo(
            videos.WithQualityCount(VideoQualityLayout.Standard.Count)));
        MapVideoSet modernVideos = videos.WithQualityCount(5);
        XDocument modern = XDocument.Parse(DashManifestWriter.GenerateFullVideo(modernVideos));
        Assert.Equal(5, modernVideos.Qualities.Count);
        Assert.All(modernVideos.Qualities.Skip(fileCount),
            quality => Assert.Same(modernVideos.Qualities[fileCount - 1], quality));
        XNamespace ns = DashNamespace;
        XElement[] entries = [.. modern.Descendants(ns + "Representation")];

        Assert.Equal(5, entries.Length);
        Assert.Equal("4", entries[4].Attribute("id")?.Value);
        Assert.Equal("video_ULTRA.hd.vp9.webm", entries[4].Element(ns + "BaseURL")?.Value);
        Assert.Equal(entries[3].Attribute("bandwidth")?.Value, entries[4].Attribute("bandwidth")?.Value);
        Assert.True(XNode.DeepEquals(entries[3].Element(ns + "SegmentBase"), entries[4].Element(ns + "SegmentBase")));
        entries[4].Remove();
        Assert.True(XNode.DeepEquals(legacy, modern));
        Assert.Equal(fileCount, videos.Qualities.Count);
    }

    [Fact]
    public void FifthVp9Entry_DoesNotChangeVp8OrPreviews()
    {
        MapVideoSet vp8 = ServerMapTestData.CreateVideoSet("vp8", "https://cdn.test/video");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DashManifestWriter.GenerateFullVideo(vp8.WithQualityCount(5)));
        Assert.Equal(4, XDocument.Parse(DashManifestWriter.GenerateFullVideo(vp8))
            .Descendants(XName.Get("Representation", DashNamespace)).Count());
        MapVideoSet vp9 = ServerMapTestData.CreateVideoSet("vp9", "https://cdn.test/preview");
        XDocument preview = XDocument.Parse(DashManifestWriter.GeneratePreview([.. vp9.Qualities.Select(quality => quality.Data)]));
        Assert.Equal(4, preview.Descendants(XName.Get("Representation", DashNamespace)).Count());
        Assert.DoesNotContain("video_ULTRA.hd.vp9.webm", preview.ToString());
    }

    [Fact]
    public void FullVp9Manifest_UsesRealFifthFileAndKeepsHighestAvailableForLegacy()
    {
        MapVideoQuality[] source = [.. Enumerable.Range(1, 5).Select(index =>
            new MapVideoQuality(new WebmData { Duration = 30, Bitrate = index * 1000,
                Start = index * 100, End = index * 100 + 50 }, $"https://cdn.test/{index}.webm"))];
        MapVideoSet legacy = new MapVideoSet(MapVideoCodec.Vp9, source)
            .WithQualityCount(VideoQualityLayout.Standard.Count);
        MapVideoSet modern = legacy.WithQualityCount(5);
        Assert.Same(source[4], legacy.Qualities[3]);
        Assert.Equal(source, modern.Qualities);
        Assert.Equal(legacy.Qualities, modern.WithQualityCount(4).Qualities);
        XNamespace ns = DashNamespace;
        XElement[] entries = [.. XDocument.Parse(DashManifestWriter.GenerateFullVideo(modern))
            .Descendants(ns + "Representation")];
        Assert.Equal(5, entries.Length);
        for (int index = 0; index < entries.Length; index++)
        {
            Assert.Equal(source[index].Data.Bitrate.ToString(), entries[index].Attribute("bandwidth")!.Value);
            Assert.Equal($"{source[index].Data.Start}-{source[index].Data.End}",
                entries[index].Element(ns + "SegmentBase")!.Attribute("indexRange")!.Value);
            Assert.Equal(VideoQualityLayout.FullVideoAssetNames(modern.Codec, modern.Qualities.Count)[index], entries[index].Element(ns + "BaseURL")!.Value);
        }
    }
}
