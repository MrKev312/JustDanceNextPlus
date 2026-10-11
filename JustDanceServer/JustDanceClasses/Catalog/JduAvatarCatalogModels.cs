namespace JustDanceServer.JustDanceClasses.Catalog;

public enum JduCustomizableItemStatus
{
    None = 0,
    Locked = 1,
    Hidden = 2,
    Unlocked = 3
}

public enum JduCustomizableItemUnlockType
{
    None = 0,
    Mojo = 1,
    Date = 2,
    Map = 3,
    Wdf = 4,
    PlayerJourney = 5,
    Uplay = 6,
    Tournament = 7,
    Amiibo = 8,
    Quest = 9,
    Progression = 10,
    WdfBoss = 11,
    WdfTournamentTop1 = 12,
    WdfTournamentTop2 = 13,
    WdfTournamentTop3 = 14,
    WdfTournamentParticipation = 15,
    Uplay2 = 16,
    JdRank = 17,
    Gacha = 18,
    Legacy = 19,
    UnlockedByDefault = 20,
    Objective = 21,
    JduReward = 22,

    // These values occur in the imported catalog but are newer than the
    // JD2022 enum available to us. The names deliberately do not invent
    // semantics while still preserving the stable wire values.
    Unresolved24 = 24,
    Unresolved25 = 25
}

public sealed record JduAvatarManifestEntry
{
    public int? JdVersion { get; init; }
    public uint? Coach { get; init; }
    public required string Image { get; init; }
    public string? Sound { get; init; }
    public JduCustomizableItemStatus? Status { get; init; }
    public JduCustomizableItemUnlockType? UnlockType { get; init; }
    public int? Price { get; init; }
    public string? RelatedSong { get; init; }
    public string? RelatedQuest { get; init; }
    public string? RelatedWdfBoss { get; init; }
}

public sealed record JduAvatarDefinition(
    uint Id,
    int JdVersion,
    string? MapName,
    uint? Coach,
    string ImagePath,
    string? SoundFamily,
    IReadOnlyList<string> SoundPaths,
    JduCustomizableItemStatus Status,
    JduCustomizableItemUnlockType UnlockType,
    int Price,
    string? RelatedSong,
    string? RelatedQuest,
    string? RelatedWdfBoss);
