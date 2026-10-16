using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Utilities;

using System.Globalization;
using System.Security;
using System.Text;

namespace JustDanceServer.Services;

public sealed record JduVideoPreviewProjection(
    string Mpd,
    Dictionary<string, string> Urls);

public static class JduVideoPreviewFactory
{
    public static JduVideoPreviewProjection Create(
        JustDanceSongDBEntry song,
        MapMediaAssets media,
        UrlSettings urlSettings,
        MapVideoCodec codec)
    {
        string codecName = codec.ToCodecName();
        MapVideoSet videos = media.GetPreviewVideos(codecName);
        WebmData[] videoData = [.. videos.Qualities.Select(quality => quality.Data)];
        Dictionary<string, string> urls = [with(StringComparer.Ordinal)];
        string[] logicalUrls = new string[videoData.Length];

        for (int index = 0; index < videoData.Length; index++)
        {
            string sourceUrl = videos.Qualities[index].Url;
            string logicalUrl =
                $"jmcs://jd-contents/{song.MapName}/{song.MapName}_MapPreviewNoSoundCrop_{VideoQualityLayout.Standard[index]}.{codecName}.webm";

            logicalUrls[index] = logicalUrl;
            urls[logicalUrl] = JduContentUrlResolver.RebaseToCdn(sourceUrl, urlSettings);
        }

        return new JduVideoPreviewProjection(CreateMpd(videoData, logicalUrls, codecName), urls);
    }

    private static string CreateMpd(WebmData[] videoData, string[] logicalUrls, string codec)
    {
        string duration = videoData[0].Duration.ToString(CultureInfo.InvariantCulture);
        StringBuilder mpd = new();
        mpd.Append($$"""
            <?xml version="1.0"?>
            <MPD xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="urn:mpeg:DASH:schema:MPD:2011" xsi:schemaLocation="urn:mpeg:DASH:schema:MPD:2011" type="static" mediaPresentationDuration="PT{{duration}}S" minBufferTime="PT1S" profiles="urn:webm:dash:profile:webm-on-demand:2012">
              <Period id="0" start="PT0S" duration="PT{{duration}}S">
                <AdaptationSet id="0" mimeType="video/webm" codecs="{{codec}}" lang="eng" maxWidth="720" maxHeight="370" subsegmentAlignment="true" subsegmentStartsWithSAP="1" bitstreamSwitching="true">

            """);

        for (int index = 0; index < videoData.Length; index++)
        {
            WebmData webm = videoData[index];
            string logicalUrl = SecurityElement.Escape(logicalUrls[index]) ?? logicalUrls[index];
            mpd.Append($$"""
                  <Representation id="{{index}}" bandwidth="{{webm.Bitrate}}">
                    <BaseURL>{{logicalUrl}}</BaseURL>
                    <SegmentBase indexRange="{{webm.Start}}-{{webm.End}}">
                      <Initialization range="0-{{webm.Start}}" />
                    </SegmentBase>
                  </Representation>

            """);
        }

        mpd.Append("""
                </AdaptationSet>
              </Period>
            </MPD>
            """);

        return mpd.ToString();
    }
}
