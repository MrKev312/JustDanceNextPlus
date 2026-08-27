using JustDanceServer.Utilities;

using System.Globalization;
using System.Text;

namespace JustDanceServer.Services;

public static class DashManifestWriter
{
	public static string GeneratePreview(IReadOnlyList<WebmData> webms, MapVideoCodec codec = MapVideoCodec.Vp9)
		=> Generate(
			webms,
			codec,
			"videoPreview_",
			codec.ToCodecName(),
			maxWidth: 768,
			maxHeight: 432);

	public static string GenerateFullVideo(MapVideoSet videos)
		=> Generate(
			[.. videos.Qualities.Select(quality => quality.Data)],
			videos.Codec,
			"video_",
			videos.Codec == MapVideoCodec.Vp8 ? "hd" : "vp9",
			maxWidth: 1920,
			maxHeight: 1080);

	private static string Generate(
		IReadOnlyList<WebmData> webms,
		MapVideoCodec codec,
		string baseUrlPrefix,
		string assetSuffix,
		int maxWidth,
		int maxHeight)
	{
		if (webms.Count == 0)
			return "";
		if (webms.Count > MapVideoSet.QualityCount)
			throw new InvalidOperationException("Too many video files");

		string duration = webms[0].Duration.ToString(CultureInfo.InvariantCulture);
		StringBuilder builder = new();
		string codecName = codec.ToCodecName();
		builder.Append($"""
			<?xml version="1.0"?>
			<MPD xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns="urn:mpeg:DASH:schema:MPD:2011" xsi:schemaLocation="urn:mpeg:DASH:schema:MPD:2011" type="static" mediaPresentationDuration="PT{duration}S" minBufferTime="PT10S" profiles="urn:webm:dash:profile:webm-on-demand:2012">
			<Period id="0" start="PT0S" duration="PT{duration}S">
			<AdaptationSet id="0" mimeType="video/webm" codecs="{codecName}" lang="eng" maxWidth="{maxWidth}" maxHeight="{maxHeight}" subsegmentAlignment="true" subsegmentStartsWithSAP="1" bitstreamSwitching="true">

			""");

		for (int index = 0; index < webms.Count; index++)
		{
			WebmData webm = webms[index];
			builder.Append($"""
				<Representation id="{index}" bandwidth="{webm.Bitrate}">
				<BaseURL>{baseUrlPrefix}{MapVideoSet.QualityNames[index]}.{assetSuffix}.webm</BaseURL>
				<SegmentBase indexRange="{webm.Start}-{webm.End}">
				<Initialization range="0-{webm.Start}" />
				</SegmentBase>
				</Representation>

				""");
		}

		builder.Append("""
			</AdaptationSet>
			</Period>
			</MPD>

			""");
		return builder.ToString();
	}
}
