using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Services;

public enum ChallengeMatchType
{
	Ranked,
	Friendly
}

public enum ChallengeMatchState
{
	InProgress,
	Completed,
	UpdatePending
}

public sealed record ChallengeRoundInfo(
	Guid UgcId,
	string MapName,
	long CreationTime,
	Guid Dancer,
	int UgcScore,
	int DancerScore,
	Guid Winner);

public sealed record ChallengeMatchInfo(
	IReadOnlyList<string> Challengers,
	IReadOnlyList<ChallengeRoundInfo> Rounds,
	string ErrorCode,
	long CreationTime,
	long UpdationTime,
	string Type,
	string State,
	Guid MatchId);

public interface IChallengeMatchService
{
	ValueTask<Guid?> FindChallengeForMapAsync(Guid profileId, string mapName);
	ValueTask<ChallengeMatchInfo?> CreateAsync(
		Guid profileId,
		ChallengeMatchType type,
		Guid? opponentProfileId,
		Guid? ugcId);
	ChallengeMatchInfo? StartRound(Guid matchId, Guid profileId);
	ValueTask<ChallengeMatchInfo?> FinalizeRoundAsync(
		Guid matchId,
		Guid profileId,
		int score,
		string platform);
	bool Delete(Guid matchId, Guid profileId);
}

public sealed class ChallengeMatchService(
	IServiceScopeFactory scopeFactory,
	TimeProvider timeProvider) : IChallengeMatchService
{
	private static readonly TimeSpan MatchLifetime = TimeSpan.FromHours(24);
	private static readonly TimeSpan SeedLifetime = TimeSpan.FromMinutes(15);
	private readonly Lock stateLock = new();
	private readonly Dictionary<Guid, MatchState> matches = [];
	private readonly Dictionary<Guid, ChallengeSeed> seeds = [];

	public async ValueTask<Guid?> FindChallengeForMapAsync(Guid profileId, string mapName)
	{
		ScoreCandidate? candidate = await FindCandidateAsync(profileId, mapName, null);
		if (candidate is null)
			return null;

		lock (stateLock)
		{
			CleanupLocked();
			Guid ugcId = Guid.NewGuid();
			seeds[ugcId] = new ChallengeSeed(profileId, candidate, UtcNow.Add(SeedLifetime));
			return ugcId;
		}
	}

	public async ValueTask<ChallengeMatchInfo?> CreateAsync(
		Guid profileId,
		ChallengeMatchType type,
		Guid? opponentProfileId,
		Guid? ugcId)
	{
		ScoreCandidate? candidate = null;
		if (ugcId is Guid seedId)
		{
			lock (stateLock)
			{
				CleanupLocked();
				if (seeds.Remove(seedId, out ChallengeSeed? seed)
					&& seed.ProfileId == profileId)
					candidate = seed.Candidate;
			}

			if (candidate is null)
				return null;
		}

		candidate ??= await FindCandidateAsync(profileId, null, opponentProfileId);
		if (candidate is null
			|| candidate.ProfileId == profileId
			|| (opponentProfileId is Guid opponent && candidate.ProfileId != opponent))
			return null;

		DateTimeOffset now = UtcNow;
		MatchState match = new()
		{
			MatchId = Guid.NewGuid(),
			ProfileId = profileId,
			OpponentProfileId = candidate.ProfileId,
			Type = type,
			CreatedAt = now,
			UpdatedAt = now,
			ExpiresAt = now.Add(MatchLifetime),
			Round = new ChallengeRoundInfo(
				ugcId ?? Guid.Empty,
				candidate.MapName,
				now.ToUnixTimeSeconds(),
				candidate.ProfileId,
				candidate.Score,
				0,
				Guid.Empty)
		};
		lock (stateLock)
		{
			CleanupLocked();
			matches.Add(match.MatchId, match);
			return ToInfo(match);
		}
	}

	public ChallengeMatchInfo? StartRound(Guid matchId, Guid profileId)
	{
		lock (stateLock)
		{
			CleanupLocked();
			if (!matches.TryGetValue(matchId, out MatchState? match)
				|| match.ProfileId != profileId
				|| match.State == ChallengeMatchState.Completed)
				return null;

			match.Started = true;
			match.UpdatedAt = UtcNow;
			return ToInfo(match);
		}
	}

	public async ValueTask<ChallengeMatchInfo?> FinalizeRoundAsync(
		Guid matchId,
		Guid profileId,
		int score,
		string platform)
	{
		MatchState match;
		lock (stateLock)
		{
			CleanupLocked();
			if (!matches.TryGetValue(matchId, out match!)
				|| match.ProfileId != profileId
				|| !match.Started)
				return null;
			if (match.State == ChallengeMatchState.Completed)
				return ToInfo(match);
			if (match.State == ChallengeMatchState.UpdatePending)
				return ToInfo(match);

			match.State = ChallengeMatchState.UpdatePending;
			match.UpdatedAt = UtcNow;
		}

		int normalizedScore = Math.Clamp(score, 0, 13_333);
		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		IPlayerActivityService activity = scope.ServiceProvider.GetRequiredService<IPlayerActivityService>();
		bool persisted;
		try
		{
			persisted = await activity.RecordChallengeAsync(matchId, profileId, match.Round.MapName, normalizedScore, platform);
		}
		catch
		{
			lock (stateLock)
				match.State = ChallengeMatchState.InProgress;
			throw;
		}

		if (!persisted)
		{
			lock (stateLock)
			{
				if (matches.TryGetValue(matchId, out MatchState? current)
					&& current.State == ChallengeMatchState.UpdatePending)
					current.State = ChallengeMatchState.InProgress;
			}

			return null;
		}

		lock (stateLock)
		{
			if (!matches.TryGetValue(matchId, out MatchState? current))
				return null;
			Guid winner = normalizedScore == current.Round.UgcScore
				? Guid.Empty
				: normalizedScore > current.Round.UgcScore ? profileId : current.OpponentProfileId;
			current.Round = current.Round with
			{
				DancerScore = normalizedScore,
				Winner = winner
			};
			current.State = ChallengeMatchState.Completed;
			current.UpdatedAt = UtcNow;
			return ToInfo(current);
		}
	}

	public bool Delete(Guid matchId, Guid profileId)
	{
		lock (stateLock)
		{
			CleanupLocked();
			return matches.TryGetValue(matchId, out MatchState? match)
				&& (match.ProfileId == profileId || match.OpponentProfileId == profileId)
				&& matches.Remove(matchId);
		}
	}

	private async ValueTask<ScoreCandidate?> FindCandidateAsync(
		Guid profileId,
		string? mapName,
		Guid? opponentProfileId)
	{
		if (profileId == Guid.Empty || opponentProfileId == profileId)
			return null;

		await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
		UserDataContext dbContext = scope.ServiceProvider.GetRequiredService<UserDataContext>();
		IQueryable<MapStats> query = dbContext.MapScores.AsNoTracking()
			.Where(value => value.ProfileId != profileId && value.HighScore > 0);
		if (!string.IsNullOrWhiteSpace(mapName))
			query = query.Where(value => value.MapName == mapName);
		if (opponentProfileId is Guid opponent)
			query = query.Where(value => value.ProfileId == opponent);

		return await query
			.OrderByDescending(value => value.UpdatedAt)
			.ThenByDescending(value => value.HighScore)
			.Select(value => new ScoreCandidate(value.ProfileId, value.MapName, value.HighScore))
			.FirstOrDefaultAsync();
	}

	private void CleanupLocked()
	{
		DateTimeOffset now = UtcNow;
		foreach (Guid id in matches.Where(value => value.Value.ExpiresAt <= now).Select(value => value.Key).ToArray())
			matches.Remove(id);
		foreach (Guid id in seeds.Where(value => value.Value.ExpiresAt <= now).Select(value => value.Key).ToArray())
			seeds.Remove(id);
	}

	private static ChallengeMatchInfo ToInfo(MatchState match)
		=> new(
			[match.ProfileId.ToString(), match.OpponentProfileId.ToString()],
			[match.Round],
			"EMatchErrorType_None",
			match.CreatedAt.ToUnixTimeSeconds(),
			match.CreatedAt.ToUnixTimeSeconds(),
			match.Type == ChallengeMatchType.Ranked ? "EMatchType_Ranked" : "EMatchType_Friendly",
			match.State switch
			{
				ChallengeMatchState.Completed => "EMatchState_Completed",
				ChallengeMatchState.UpdatePending => "EMatchState_UpdatePending",
				_ => "EMatchState_InProgress"
			},
			match.MatchId);

	private DateTimeOffset UtcNow => timeProvider.GetUtcNow();

	private sealed record ScoreCandidate(Guid ProfileId, string MapName, int Score);
	private sealed record ChallengeSeed(
		Guid ProfileId,
		ScoreCandidate Candidate,
		DateTimeOffset ExpiresAt);

	private sealed class MatchState
	{
		public required Guid MatchId { get; init; }
		public required Guid ProfileId { get; init; }
		public required Guid OpponentProfileId { get; init; }
		public required ChallengeMatchType Type { get; init; }
		public required DateTimeOffset CreatedAt { get; init; }
		public required DateTimeOffset UpdatedAt { get; set; }
		public required DateTimeOffset ExpiresAt { get; init; }
		public required ChallengeRoundInfo Round { get; set; }
		public ChallengeMatchState State { get; set; }
		public bool Started { get; set; }
	}
}
