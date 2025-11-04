using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record SongDBTypeSet(string HostUrl)
{
    public string SongdbUrl { get; } = $"https://{HostUrl}/songdb/songDB";
    public Songoffers SongOffers { get; init; } = new();
}

public record Songoffers
{
    public IReadOnlySet<Guid> FreeSongs { get; init; } = ImmutableHashSet<Guid>.Empty;
    public IReadOnlySet<Guid> HiddenSongs { get; init; } = ImmutableHashSet<Guid>.Empty;
    public IReadOnlySet<Guid> LocalTracks { get; init; } = ImmutableHashSet<Guid>.Empty;
    public IReadOnlyDictionary<string, Pack> Claims { get; init; } = ImmutableDictionary<string, Pack>.Empty;
    public IReadOnlySet<Guid> DownloadableSongs { get; init; } = ImmutableHashSet<Guid>.Empty;
}

public record Pack
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int DescriptionLocId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AllowSharing { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int FreeTrialDurationMinutes { get; init; }

    public IReadOnlySet<Guid> RewardIds { get; init; } = ImmutableHashSet<Guid>.Empty;
    public IReadOnlySet<Guid> SongIds { get; init; } = ImmutableHashSet<Guid>.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlySet<string>? SongPackIds { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UnlocksFullVersion { get; init; }
}
