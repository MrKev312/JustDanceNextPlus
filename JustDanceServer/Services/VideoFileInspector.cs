using JustDanceServer.Utilities;

namespace JustDanceServer.Services;

public interface IVideoFileInspector
{
    WebmData[] GetVideoData(string videoFolder);
}

public sealed class VideoFileInspector(
    IFileSystem fileSystem,
    IWebmExtractor webmExtractor,
    ILogger<VideoFileInspector> logger) : IVideoFileInspector
{
    public WebmData[] GetVideoData(string videoFolder)
    {
        string[] videoFiles = fileSystem.GetFiles(videoFolder, "*.webm");

        if (videoFiles.Length == 0)
            throw new FileNotFoundException($"Missing video file in {videoFolder}");
        if (videoFiles.Length > MapVideoSet.QualityCount)
            throw new InvalidOperationException($"Too many video files in {videoFolder}");

        WebmData[] webmData = new WebmData[videoFiles.Length];
        Parallel.For(0, videoFiles.Length, index =>
        {
            WebmData data = webmExtractor.GetCuesInfo(videoFiles[index]);
            long fileLength = fileSystem.GetFileLength(videoFiles[index]);

            if (CuesAppearAtEnd(data, fileLength))
            {
                logger.LogWarning(
                    "WebM file '{VideoFile}' has cues at the end ({CueStart}-{CueEnd} of {FileLength} bytes). Run mkclean or regenerate with front-loaded cues.",
                    videoFiles[index],
                    data.Start,
                    data.End,
                    fileLength);
            }

            webmData[index] = data;
        });

        List<WebmData> qualities = [.. webmData.OrderBy(data => data.Bitrate)];
        while (qualities.Count < MapVideoSet.QualityCount)
            qualities.Add(qualities[^1]);

        return [.. qualities];
    }

    internal static bool CuesAppearAtEnd(WebmData data, long fileLength)
    {
        const long tailToleranceBytes = 4096;
        return fileLength > tailToleranceBytes
            && data.Start > tailToleranceBytes
            && data.End >= fileLength - tailToleranceBytes;
    }
}
