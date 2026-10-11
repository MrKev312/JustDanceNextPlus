using System.Text.Json.Serialization;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record ClassicLeaderboardResponse
{
    public IReadOnlyList<ClassicLeaderboardEntry> Entries { get; init; } = [];
    public long PlayerCount { get; init; }
}

public sealed record ClassicLeaderboardEntry
{
    public string Name { get; init; } = string.Empty;
    public int Country { get; init; }
    public uint Avatar { get; init; }
    public int Score { get; init; }
    public long Rank { get; init; }
    public int JdPoints { get; init; }
    public uint Alias { get; init; }
    public int AliasGender { get; init; }

    [JsonPropertyName("portraitborder")]
    public uint PortraitBorder { get; init; }

    public Guid ProfileId { get; init; }
    public string PlatformId { get; init; } = string.Empty;
}
