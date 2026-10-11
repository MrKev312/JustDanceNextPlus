namespace JustDanceServer.JustDanceClasses.Database.Profile;

public enum PlayerActivitySource
{
    JduMapEnded,
    JdNextMapPlayed,
    Challenge
}

public enum ScoreAttribution
{
    Unknown,
    Individual,
    SessionBest,
    Cooperative
}

public sealed class PlayerMapPlay
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public PlayerActivitySource Source { get; set; }
    public string SourceEventKey { get; set; } = string.Empty;
    public ProgressionFamily Family { get; set; }
    public int? GameYear { get; set; }
    public string Platform { get; set; } = string.Empty;
    public DateTime PlayedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public int Score { get; set; }
    public string Mode { get; set; } = string.Empty;
    public ScoreAttribution Attribution { get; set; }
    public bool? Completed { get; set; }
    public bool WeeklyEligible { get; set; }
}

public sealed class PlayerMapFavorite
{
    public Guid ProfileId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public DateTime FavoritedAtUtc { get; set; }
}

// These are local save-file ordinals, not dates or globally comparable counters.
public sealed class PlayerMapOrdinal
{
    public Guid ProfileId { get; set; }
    public string SourceScope { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public long Ordinal { get; set; }
    public DateTime ObservedAtUtc { get; set; }
}
