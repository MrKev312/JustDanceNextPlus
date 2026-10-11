using System.ComponentModel.DataAnnotations;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record JduHomeRequest
{
    [Range(0, 100)]
    public int RequestedTileCount { get; init; } = 20;
    [Required, MaxLength(100)]
    public JduHomeTile[] TileHistory { get; init; } = [];
    [Required, MaxLength(100)]
    public Guid[] WatchedVideos { get; init; } = [];
    public long TimestampLastRequest { get; init; }
    public long TimestampLastManualContent { get; init; }
    public long TimestampLastMapContent { get; init; }
    public long TimestampLastVideoContent { get; init; }
    public long TimestampLastPlaylistContent { get; init; }
    public long TimestampLastLocalTrack { get; init; }
    public bool UplayConnected { get; init; }
}
