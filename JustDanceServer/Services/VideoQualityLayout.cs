namespace JustDanceServer.Services;

// These names describe the client protocol, not a limit on stored video files.
public static class VideoQualityLayout
{
    public static IReadOnlyList<string> Standard { get; } =
        Array.AsReadOnly<string>(["LOW", "MID", "HIGH", "ULTRA"]);

    public static IReadOnlyList<string> FullVp9 { get; } =
        Array.AsReadOnly<string>(["LOW", "MID", "HIGH", "ULTRA", "ULTRA.hd"]);

    public static IReadOnlyList<string> FullVideoAssetNames(MapVideoCodec codec, int count)
    {
        IReadOnlyList<string> names = codec == MapVideoCodec.Vp9 ? FullVp9 : Standard;

        if (count < Standard.Count || count > names.Count)
            throw new ArgumentOutOfRangeException(nameof(count), "The requested layout has no defined protocol asset names.");

        string suffix = codec == MapVideoCodec.Vp8 ? "hd" : "vp9";

        return names.Take(count).Select(name => $"video_{name}.{suffix}.webm").ToArray();
    }
}
