namespace JustDanceServer.JustDanceClasses.Database.Profile;

public enum ProgressionFamily
{
    JDPlus,
    JDUnlimited
}

public sealed class PlayerProgression
{
    public Guid ProfileId { get; set; }
    public ProgressionFamily Family { get; set; }
    public int Xp { get; set; } = 1;
    public int Level { get; set; } = 1;
    public int Prestige { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlayerUnlock
{
    public Guid ProfileId { get; set; }
    public ProgressionFamily Family { get; set; }
    public string UnlockKey { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlayerQuestProgress
{
    public Guid ProfileId { get; set; }
    public ProgressionFamily Family { get; set; }
    public string QuestKey { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlayerGachaState
{
    public Guid ProfileId { get; set; }
    public ProgressionFamily Family { get; set; }
    public int Rank { get; set; }
    public int PullCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlayerMetadata
{
    public Guid ProfileId { get; set; }
    public string CountryCode { get; set; } = "NL";
    public int CountryId { get; set; }
    public string Locale { get; set; } = "en-US";
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
