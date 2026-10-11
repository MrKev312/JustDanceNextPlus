namespace JustDanceServer.Services;

public sealed record MapAudioLayout
{
    public MapAudioLayout(string relativeFolder, string fileExtension)
    {
        if (string.IsNullOrWhiteSpace(relativeFolder))
            throw new ArgumentException("An audio layout must have a relative folder.", nameof(relativeFolder));
        if (string.IsNullOrWhiteSpace(fileExtension) || fileExtension[0] != '.')
            throw new ArgumentException("An audio layout file extension must begin with '.'.", nameof(fileExtension));

        RelativeFolder = relativeFolder.Trim('/');
        FileExtension = fileExtension;
    }

    public string RelativeFolder { get; }
    public string FileExtension { get; }
}

public sealed record MapMediaLayout
{
    public MapMediaLayout(
        string folderName,
        MapAudioLayout previewAudio,
        MapAudioLayout fullAudio,
        IReadOnlyList<string> previewVideoCodecs)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            throw new ArgumentException("A media layout must have a folder name.", nameof(folderName));
        ArgumentNullException.ThrowIfNull(previewAudio);
        ArgumentNullException.ThrowIfNull(fullAudio);
        ArgumentNullException.ThrowIfNull(previewVideoCodecs);
        if (previewVideoCodecs.Count == 0 || previewVideoCodecs.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("A media layout must declare its preview video codecs.", nameof(previewVideoCodecs));

        FolderName = folderName;
        PreviewAudio = previewAudio;
        FullAudio = fullAudio;
        PreviewVideoCodecs = previewVideoCodecs;
    }

    public string FolderName { get; }
    public MapAudioLayout PreviewAudio { get; }
    public MapAudioLayout FullAudio { get; }
    public IReadOnlyList<string> PreviewVideoCodecs { get; }

    public string GetPreviewVideoFolder(string codec)
        => $"{FolderName}/Preview/{codec}";

}

public static class MapMediaLayouts
{
    public static MapMediaLayout JDPlus { get; } = new(
        "JDPlus",
        new("JDPlus/Preview", ".opus"),
        new("JDPlus/Audio", ".opus"),
        ["vp8", "vp9"]);

    public static MapMediaLayout JDUnlimited { get; } = new(
        "JDUnlimited",
        new("JDUnlimited/Preview", ".ogg"),
        new("JDUnlimited/Audio", ".ogg"),
        ["vp8", "vp9"]);

    public static IReadOnlyList<MapMediaLayout> All { get; } =
    [
        JDPlus,
        JDUnlimited
    ];
}
