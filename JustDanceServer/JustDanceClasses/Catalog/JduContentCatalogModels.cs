namespace JustDanceServer.JustDanceClasses.Catalog;

public sealed record JduSkinManifest
{
    public required uint Id { get; init; }
    public required int JdVersion { get; init; }
    public JduCustomizableItemStatus? Status { get; init; }
    public JduCustomizableItemUnlockType? UnlockType { get; init; }
    public int? Price { get; init; }
}

public sealed record JduSkinDefinition(
    uint Id,
    int JdVersion,
    string ImagePath,
    JduCustomizableItemStatus Status,
    JduCustomizableItemUnlockType UnlockType,
    int Price);

public sealed record JduPortraitBorderManifest
{
    public required uint Id { get; init; }
    public int OriginalLockStatus { get; init; }
    public required int Visibility { get; init; }
}

public sealed record JduPortraitBorderDefinition(
    uint Id,
    string BackgroundPath,
    string? ForegroundPath,
    int OriginalLockStatus,
    int Visibility);

public sealed record JduAliasManifest
{
    public required uint Id { get; init; }
    public uint? DescriptionLocId { get; init; }
    public required uint DifficultyColor { get; init; }
    public bool? RestrictedToUnlimitedSongs { get; init; }
    public required uint StringLocId { get; init; }
    public uint? StringLocIdFemale { get; init; }
    public required string Placeholder { get; init; }
    public required string Localized { get; init; }
    public required string LocalizedFemale { get; init; }
    public required string Description { get; init; }
    public required int Visibility { get; init; }
    public JduAliasUnlockManifest? Unlock { get; init; }
}

public sealed record JduAliasUnlockManifest
{
    public required string Class { get; init; }
    public required uint Description { get; init; }
    public required int MinimumValue { get; init; }
    public int? ObjectiveType { get; init; }
    public int? MinimumStars { get; init; }
}

public sealed record JduAliasDefinition(
    uint Id,
    uint? DescriptionLocId,
    uint DifficultyColor,
    bool? RestrictedToUnlimitedSongs,
    uint StringLocId,
    uint? StringLocIdFemale,
    string Placeholder,
    string Localized,
    string LocalizedFemale,
    string Description,
    int Visibility,
    JduAliasUnlockManifest? Unlock);

public sealed record JduQuestManifest
{
    public required uint Id { get; init; }
    public required string Title { get; init; }
    public required string[] Maps { get; init; }
    public bool Locked { get; init; }
}

public sealed record JduQuestDefinition(
    uint Id,
    string Title,
    IReadOnlyList<string> MapNames,
    bool Locked,
    string PhoneImagePath,
    IReadOnlyDictionary<string, JduQuestPlatformAssets> PlatformAssets);

public sealed record JduQuestPlatformAssets(
    string CoverPath,
    string LogoPath,
    string ShadedLogoPath);
