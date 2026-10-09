using System.Text.Json.Serialization;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record WdfScreenList
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "ScreenList";
	public IReadOnlyList<WdfScreen> Screens { get; init; } = [];
}

public sealed record WdfHappyHoursInfo
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "HappyHoursInfo";
	public required double Start { get; init; }
	public required double End { get; init; }
	public required bool Running { get; init; }
}

public sealed record WdfScreen
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "Screen";
	public required string Type { get; init; }
	public required double StartTime { get; init; }
	public required double EndTime { get; init; }
	public string Theme { get; init; } = "vote";
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MapName { get; init; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public WdfVoteScreenInfo? VoteInfo { get; init; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public WdfWaitingScreenInfo? WaitingScreenInfo { get; init; }
}

public sealed record WdfVoteScreenInfo
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "VoteScreenInfo";
	public required IReadOnlyList<string> VoteOptions { get; init; }
	public required double VoteStartTime { get; init; }
	public required double VoteEndTime { get; init; }
	public required double VoteResultFetchTime { get; init; }
}

public sealed record WdfWaitingScreenInfo
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "WaitingScreenInfo";
	public required double GetRecapTime { get; init; }
}

public sealed record WdfVoteResult
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "VoteResult";
	public IReadOnlyList<WdfVoteResultEntry> Entries { get; init; } = [];
}

public sealed record WdfVoteResultEntry
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "VoteResultEntry";
	public required string Name { get; init; }
	public required int Value { get; init; }
}

public sealed record WdfScoreEntry
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "ScoreEntry";
	public required Guid Pid { get; init; }
	public required string Name { get; init; }
	public int NameSuffix { get; init; }
	public int Country { get; init; }
	public uint Avatar { get; init; }
	public uint Skin { get; init; }
	public uint PortraitBorder { get; init; }
	public required double Score { get; init; }
	public required string Platform { get; init; }
	public bool TournamentBadge { get; init; }
	public long JdPoints { get; init; }
	public bool IsSubscribed { get; init; } = true;
}

public sealed record WdfOnlineRankInfo
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "WDFOnlineRankInfo";
	public int Rank { get; init; }
	public long WdfPoints { get; init; }
	[JsonPropertyName("dc")]
	public required WdfScoreEntry DancerCard { get; init; }
}

public sealed record WdfUpdateScoreResult
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "UpdateScoreResult";
	public int CurrentRank { get; init; }
	public int TotalPlayerCount { get; init; }
	public IReadOnlyList<WdfScoreEntry> ScoreEntries { get; init; } = [];
}

public sealed record WdfInitScoreResult
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "InitScoreResult";
	public int CurrentRank { get; init; }
	public double Score { get; init; }
	public required string ScoreSignature { get; init; }
}

public sealed record WdfRecapInfo
{
	[JsonPropertyName("__class")]
	public string Class { get; init; } = "RecapInfo";
	public bool RecapComputed { get; init; } = true;
	public int CurrentRank { get; init; }
	public int TotalPlayerCount { get; init; }
	public IReadOnlyList<WdfScoreEntry> RecapEntries { get; init; } = [];
}
