using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Services;

public sealed record JduAudioPreviewProjection(
    string Data,
    Dictionary<string, string> Urls);

public static class JduAudioPreviewFactory
{
    private const int PreviewDurationInSamples = 30 * 48_000;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static JduAudioPreviewProjection Create(
        JustDanceSongDBEntry song,
        MapMediaAssets media,
        UrlSettings urlSettings)
    {
        AudioPreviewTrk? track = song.AssetsMetadata.AudioPreviewTrk;
        if (track is null)
            return new JduAudioPreviewProjection(string.Empty, []);

        string httpsUrl = JduContentUrlResolver.RebaseToCdn(media.PreviewAudioUrl, urlSettings);
        string logicalUrl = $"jmcs://jd-contents/{song.MapName}/{song.MapName}_AudioPreview.ogg";
        int[] previewMarkers = CreatePreviewMarkers(track, out int previewLoopEnd);
        JduMusicTrackData data = new()
        {
            Structure = new JduMusicTrackStructure
            {
                Markers = previewMarkers,
                Signatures = [new JduMusicSignature()],
                StartBeat = track.StartBeat,
                EndBeat = track.EndBeat,
                PreviewLoopEnd = previewLoopEnd
            },
            Url = logicalUrl
        };

        return new JduAudioPreviewProjection(
            JsonSerializer.Serialize(data, SerializerOptions),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [logicalUrl] = httpsUrl
            });
    }

    private static int[] CreatePreviewMarkers(AudioPreviewTrk track, out int previewLoopEnd)
    {
        if (track.Markers.IsDefaultOrEmpty)
        {
            previewLoopEnd = 0;
            return [0];
        }

        int previewStart = Math.Clamp((int)MathF.Floor(track.PreviewEntry), 0, track.Markers.Length - 1);
        int startSample = track.Markers[previewStart];
        List<int> markers = [0];

        for (int index = previewStart + 1; index < track.Markers.Length; index++)
        {
            int offset = track.Markers[index] - startSample;
            if (offset >= PreviewDurationInSamples)
                break;

            markers.Add(offset);
        }

        previewLoopEnd = markers.Count - 1;
        return [.. markers];
    }

    private sealed record JduMusicTrackData
    {
        [JsonPropertyName("__class")]
        public string Class { get; init; } = "MusicTrackData";
        public required JduMusicTrackStructure Structure { get; init; }
        public string Path { get; init; } = string.Empty;
        public required string Url { get; init; }
    }

    private sealed record JduMusicTrackStructure
    {
        [JsonPropertyName("__class")]
        public string Class { get; init; } = "MusicTrackStructure";
        public int[] Markers { get; init; } = [];
        public JduMusicSignature[] Signatures { get; init; } = [];
        public float StartBeat { get; init; }
        public float EndBeat { get; init; }
        public int FadeStartBeat { get; init; }
        public bool UseFadeStartBeat { get; init; }
        public int FadeEndBeat { get; init; }
        public bool UseFadeEndBeat { get; init; }
        public float VideoStartTime { get; init; }
        public int PreviewEntry { get; init; }
        public int PreviewLoopStart { get; init; }
        public int PreviewLoopEnd { get; init; }
        public int Volume { get; init; }
        public int FadeInDuration { get; init; }
        public int FadeInType { get; init; }
        public int FadeOutDuration { get; init; }
        public int FadeOutType { get; init; }
    }

    private sealed record JduMusicSignature
    {
        [JsonPropertyName("__class")]
        public string Class { get; init; } = "MusicSignature";
        public int Marker { get; init; }
        public int Beats { get; init; } = 4;
    }
}
