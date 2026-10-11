using JustDanceServer.Services;
using JustDanceServer.Utilities;

using System.Xml.Linq;

namespace JustDanceServer.Tests.Services;

public sealed class DashManifestWriterTests
{
    private const string DashNamespace = "urn:mpeg:DASH:schema:MPD:2011";

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
                [.. Enumerable.Repeat(videos[0], MapVideoSet.QualityCount)
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
    }
}
