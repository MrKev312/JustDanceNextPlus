namespace JustDanceServer.Services;

public enum WdfRoomKind
{
    Edition,
    HappyHour
}

internal enum WdfPhase
{
    Vote,
    VoteLobby,
    InGame,
    Waiting,
    Recap
}

public readonly record struct WdfRoomId
{
    public const int FirstSupportedGameYear = 2016;
    public const int LastSupportedGameYear = 2022;

    private WdfRoomId(WdfRoomKind kind, int gameYear)
    {
        Kind = kind;
        GameYear = gameYear;
    }

    public WdfRoomKind Kind { get; }
    public int GameYear { get; }
    public bool IsHappyHour => Kind == WdfRoomKind.HappyHour;
    public string Name => IsHappyHour ? "HappyHour" : $"JD{GameYear}";

    public static WdfRoomId HappyHour { get; } = new(WdfRoomKind.HappyHour, 0);

    public static IReadOnlyList<WdfRoomId> All { get; } =
    [
        HappyHour,
        .. Enumerable.Range(
            FirstSupportedGameYear,
            LastSupportedGameYear - FirstSupportedGameYear + 1)
            .Select(ForEdition)
    ];

    public static WdfRoomId ForEdition(int gameYear)
    {
        if (!IsSupportedGameYear(gameYear))
            throw new ArgumentOutOfRangeException(nameof(gameYear));

        return new WdfRoomId(WdfRoomKind.Edition, gameYear);
    }

    public static bool IsSupportedGameYear(int gameYear)
        => gameYear is >= FirstSupportedGameYear and <= LastSupportedGameYear;

    public static bool TryParse(string? value, out WdfRoomId room)
    {
        room = default;
        if (string.Equals(value, HappyHour.Name, StringComparison.OrdinalIgnoreCase))
        {
            room = HappyHour;
            return true;
        }

        if (value is not { Length: 6 }
            || !value.StartsWith("jd", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(value.AsSpan(2), out int gameYear)
            || !IsSupportedGameYear(gameYear))
        {
            return false;
        }

        room = ForEdition(gameYear);
        return true;
    }

    public override string ToString() => Name;
}

internal static class WdfScoreSignatures
{
    // Captured from JD2022 NX for the exact initial-score string "0.000000".
    public const string NintendoSwitchZero =
        "WFhIhFmrL8tck9Ui0G7uVOUMCZiJ+0RKsma7Kg2FULknNHSeDml55cVDSaZ3QnpDhJ6lQBW8nLoMEL6TnJoI7EsAxiIIUa5Z3V98nPJZzB/3Q0f0NG//yd2lorcvv454iyQ692EPcpnRXW5eCw2cibxkQz24omCr2HPhEGxivlEYc8EYmVRUFMNdbPp95PLtNbW4Q3UbpBCGBgngAV1ym02A/KDjjZIk9qK3ccSpTDNUQLBLPezxdsWr4LhXayPKqVgJLna7Wj8f6Jb5kuS4u35bW2NJ44vwoY2s7HKWedkuMI7avmKF25Fm7kCcQ+6xD3J8fn+yzBVJs4qSMfoL6A==";
}

internal static class WdfPlatform
{
    public static string Normalize(string platform)
        => platform.Trim().ToLowerInvariant() switch
        {
            "switch" or "nx" => "nx",
            "uplay" or "win" or "windows" or "pc" => "pc",
            "x1" or "xone" or "xboxone" or "durango" => "xone",
            "ps4" or "orbis" => "ps4",
            "ps5" or "prospero" => "ps5",
            "wiiu" or "cafe" => "wiiu",
            "" => "unknown",
            string value => value
        };
}

internal sealed class WdfRound
{
    public required long Id { get; init; }
    public string? MapName { get; set; }
    public required IReadOnlyList<string> VoteOptions { get; init; }
    public required double StartTime { get; init; }
    public required double GameplayStartTime { get; init; }
    public double GameplayEndTime { get; set; } = double.PositiveInfinity;
    public double EndTime { get; set; } = double.PositiveInfinity;
    public bool IsResolved => MapName is not null;
    public Dictionary<Guid, string> Votes { get; } = [];
    public IReadOnlyDictionary<string, int>? VoteResult { get; set; }
}

internal sealed record WdfQueueItem(IReadOnlyList<string> Candidates);

internal sealed class WdfStandardRoomRuntime
{
    public Queue<WdfQueueItem> RequestedQueue { get; } = new();
    public List<string> ShuffleBag { get; } = [];
    public List<WdfRound> Rounds { get; } = [];
    public long NextRoundId { get; set; }
}

internal sealed record WdfRoundSnapshot(
    long Id,
    string? MapName,
    double GameplayStartTime,
    double GameplayEndTime,
    WdfPhase Phase);

internal readonly record struct WdfRoundKey(WdfRoomId Room, long RoundId);

internal sealed class WdfPlayer
{
    public required Guid PlayerId { get; init; }
    public required string Name { get; set; }
    public required string Platform { get; set; }
    public int Country { get; set; }
    public uint Avatar { get; set; }
    public uint Skin { get; set; }
    public uint PortraitBorder { get; set; }
    public long JdPoints { get; set; }
    public double Score { get; set; }
    public bool PlayRecorded { get; set; }
    public SemaphoreSlim ScoreWriteLock { get; } = new(1, 1);
}
