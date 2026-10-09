using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public sealed class WdfService(
	WdfDirector director,
	WdfHappyHourSchedule happyHours,
	IServiceScopeFactory scopeFactory,
	TimeProvider timeProvider,
	ILogger<WdfService> logger) : IWdfService
{
	private static readonly TimeSpan ActivePlayerTimeout = TimeSpan.FromMinutes(5);

	private readonly Lock stateLock = new();
	private readonly Dictionary<WdfRoundKey, Dictionary<Guid, WdfPlayer>> roundPlayers = [];
	private readonly WdfSessionTracker sessions = new(ActivePlayerTimeout);

	public WdfRoomId GetAssignedRoom(int gameYear)
	{
		if (!WdfRoomId.IsSupportedGameYear(gameYear))
			throw new ArgumentOutOfRangeException(nameof(gameYear));

		return happyHours.IsRunning
			? WdfRoomId.HappyHour
			: WdfRoomId.ForEdition(gameYear);
	}

	public bool TryResolveRoom(string requestedRoom, int gameYear, out WdfRoomId room)
	{
		room = default;
		if (!WdfRoomId.IsSupportedGameYear(gameYear)
			|| !WdfRoomId.TryParse(requestedRoom, out WdfRoomId requested)
			|| (!requested.IsHappyHour && requested.GameYear != gameYear))
		{
			return false;
		}

		// Accept the previously assigned route across a Happy Hour boundary, but
		// always project it onto the currently active room.
		room = GetAssignedRoom(gameYear);
		return true;
	}

	public WdfCurrentMap? GetCurrentMap(WdfRoomId room)
		=> director.GetCurrentMap(room);

	public WdfHappyHoursInfo GetHappyHours()
		=> happyHours.GetInfo();

	public WdfScreenList GetScreens(WdfRoomId room, double? startTime, Guid? playerId = null)
	{
		if (playerId is Guid id)
			Touch(room, id);

		return director.GetScreens(room, startTime);
	}

	public WdfVoteResult GetVoteResult(WdfRoomId room)
		=> director.GetVoteResult(room);

	public bool SubmitVote(WdfRoomId room, Guid playerId, string mapName)
	{
		Touch(room, playerId);
		return director.SubmitVote(room, playerId, mapName);
	}

	public async ValueTask<WdfInitScoreResult> InitializeScoreAsync(
		WdfRoomId room,
		Guid playerId,
		string platform)
	{
		await EnsurePlayerAsync(room, playerId, platform);
		WdfRoundSnapshot? round = director.GetCurrentRound(room);
		lock (stateLock)
		{
			WdfUpdateScoreResult scores = CreateScoreResultLocked(
				ToRoundKey(room, round),
				playerId);
			return new WdfInitScoreResult
			{
				CurrentRank = scores.CurrentRank,
				Score = 0,
				ScoreSignature = WdfScoreSignatures.NintendoSwitchZero
			};
		}
	}

	public async ValueTask<WdfUpdateScoreResult> UpdateScoreAsync(
		WdfRoomId room,
		Guid playerId,
		string platform,
		double score)
	{
		if (playerId == Guid.Empty || !double.IsFinite(score))
			return new WdfUpdateScoreResult();

		await EnsurePlayerAsync(room, playerId, platform);
		WdfRoundSnapshot? round = director.GetCurrentRound(room);
		if (round?.MapName is not string mapName)
			return new WdfUpdateScoreResult();

		WdfRoundKey key = new(room, round.Id);
		WdfPlayer player;
		double normalizedScore = NormalizeScore(score);
		lock (stateLock)
		{
			if (GetPlayerLocked(key, playerId) is not WdfPlayer currentPlayer)
				return new WdfUpdateScoreResult();
			player = currentPlayer;
			player.Score = Math.Max(player.Score, normalizedScore);
		}

		await player.ScoreWriteLock.WaitAsync();
		try
		{
			if (await PersistScoreAsync(playerId, platform, mapName, normalizedScore, !player.PlayRecorded))
				player.PlayRecorded = true;
		}
		finally
		{
			player.ScoreWriteLock.Release();
		}

		WdfRoundSnapshot? current = director.GetCurrentRound(room);
		lock (stateLock)
			return CreateScoreResultLocked(ToRoundKey(room, current), playerId);
	}

	public WdfRecapInfo GetRecap(WdfRoomId room, Guid playerId)
	{
		WdfRoundSnapshot? round = director.GetCurrentRound(room);
		lock (stateLock)
		{
			WdfUpdateScoreResult scores = CreateScoreResultLocked(
				ToRoundKey(room, round),
				playerId);
			return new WdfRecapInfo
			{
				CurrentRank = scores.CurrentRank,
				TotalPlayerCount = scores.TotalPlayerCount,
				RecapEntries = scores.ScoreEntries
			};
		}
	}

	public WdfSessionSummary GetSessionSummary(Guid playerId)
	{
		lock (stateLock)
			return sessions.GetSummary(playerId, timeProvider.GetUtcNow());
	}

	public void BeginSession(WdfRoomId room, Guid playerId)
	{
		if (playerId == Guid.Empty)
			return;

		lock (stateLock)
			sessions.Begin(room, playerId, timeProvider.GetUtcNow());
	}

	public void Touch(WdfRoomId room, Guid playerId)
	{
		if (playerId == Guid.Empty)
			return;

		lock (stateLock)
			sessions.Touch(room, playerId, timeProvider.GetUtcNow());
	}

	public void Leave(WdfRoomId room, Guid playerId)
	{
		lock (stateLock)
			sessions.Leave(room, playerId);
	}

	public int GetActivePlayerCount(WdfRoomId room)
	{
		lock (stateLock)
			return sessions.GetActivePlayerCount(room, timeProvider.GetUtcNow());
	}

	private async ValueTask EnsurePlayerAsync(WdfRoomId room, Guid playerId, string platform)
	{
		if (playerId == Guid.Empty)
			return;

		Touch(room, playerId);
		WdfRoundSnapshot? round = director.GetCurrentRound(room);
		if (round is null)
			return;

		WdfRoundKey key = new(room, round.Id);
		bool loadProfile;
		lock (stateLock)
		{
			Dictionary<Guid, WdfPlayer> players = GetPlayersLocked(key);
			loadProfile = !players.ContainsKey(playerId);
			if (loadProfile)
			{
				players[playerId] = new WdfPlayer
				{
					PlayerId = playerId,
					Name = "Player",
					Platform = WdfPlatform.Normalize(platform)
				};
			}
		}

		if (!loadProfile)
			return;

		try
		{
			await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
			IUserProfileService profiles = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
			IClassicProfileService classicProfiles = scope.ServiceProvider.GetRequiredService<IClassicProfileService>();
			IPlayerMetadataService metadataService = scope.ServiceProvider.GetRequiredService<IPlayerMetadataService>();
			Profile? profile = await profiles.GetProfileByIdAsync(playerId);
			ClassicProfile? classic = await classicProfiles.GetClassicProfileAsync(playerId);
			PlayerMetadata metadata = await metadataService.GetAsync(playerId);

			lock (stateLock)
			{
				if (GetPlayerLocked(key, playerId) is not WdfPlayer player)
					return;

				player.Name = classic?.Name ?? profile?.Dancercard.Name ?? player.Name;
				player.Country = metadata.CountryId != 0 ? metadata.CountryId : classic?.CountryId ?? 0;
				player.Avatar = classic?.AvatarId ?? 0;
				player.Skin = classic?.SkinId ?? 0;
				player.PortraitBorder = classic?.PortraitBorderId ?? 0;
				player.JdPoints = classic?.JdPoints ?? 0;
				player.Platform = WdfPlatform.Normalize(classic?.Platform ?? platform);
				sessions.SetCountry(playerId, player.Country);
			}
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Failed to load WDF dancer card for {PlayerId}", playerId);
		}
	}

	private async ValueTask<bool> PersistScoreAsync(
		Guid playerId,
		string platform,
		string mapName,
		double normalizedScore,
		bool firstSubmission)
	{
		int databaseScore = Math.Clamp((int)Math.Round(normalizedScore * 13_333), 0, 13_333);
		if (databaseScore == 0)
			return false;
		try
		{
			await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
			IScoreService scores = scope.ServiceProvider.GetRequiredService<IScoreService>();
			ScoreUpdateResult result = firstSubmission
				? await scores.RecordMapPlayAsync(
					new HighscorePerformance(), databaseScore, playerId, mapName, platform)
				: await scores.MergeMapScoreAsync(
					new HighscorePerformance(), databaseScore, 0, playerId, mapName, platform);
			if (!result.Success)
				logger.LogWarning("Failed to persist WDF score for {PlayerId} on {MapName}", playerId, mapName);
			return result.Success;
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Failed to persist WDF score for {PlayerId} on {MapName}", playerId, mapName);
			return false;
		}
	}

	private Dictionary<Guid, WdfPlayer> GetPlayersLocked(WdfRoundKey key)
	{
		foreach (WdfRoundKey staleKey in roundPlayers.Keys
			.Where(candidate => candidate.Room == key.Room && candidate.RoundId < key.RoundId - 2)
			.ToArray())
		{
			roundPlayers.Remove(staleKey);
		}

		if (!roundPlayers.TryGetValue(key, out Dictionary<Guid, WdfPlayer>? players))
		{
			players = [];
			roundPlayers[key] = players;
		}

		return players;
	}

	private WdfPlayer? GetPlayerLocked(WdfRoundKey key, Guid playerId)
		=> roundPlayers.TryGetValue(key, out Dictionary<Guid, WdfPlayer>? players)
			&& players.TryGetValue(playerId, out WdfPlayer? player)
				? player
				: null;

	private WdfUpdateScoreResult CreateScoreResultLocked(WdfRoundKey? key, Guid playerId)
	{
		if (key is not WdfRoundKey roundKey
			|| !roundPlayers.TryGetValue(roundKey, out Dictionary<Guid, WdfPlayer>? players))
		{
			return new WdfUpdateScoreResult();
		}

		WdfScoreEntry[] entries = [.. players.Values
			.OrderByDescending(player => player.Score)
			.ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
			.ThenBy(player => player.PlayerId)
			.Select(ToScoreEntry)];
		int rank = players.TryGetValue(playerId, out WdfPlayer? player)
			? players.Values.Count(candidate => candidate.Score > player.Score) + 1
			: 0;
		return new WdfUpdateScoreResult
		{
			CurrentRank = rank,
			TotalPlayerCount = entries.Length,
			ScoreEntries = entries
		};
	}

	private static WdfScoreEntry ToScoreEntry(WdfPlayer player)
		=> new()
		{
			Pid = player.PlayerId,
			Name = player.Name,
			Country = player.Country,
			Avatar = player.Avatar,
			Skin = player.Skin,
			PortraitBorder = player.PortraitBorder,
			Score = player.Score,
			Platform = player.Platform,
			JdPoints = player.JdPoints
		};

	private static WdfRoundKey? ToRoundKey(WdfRoomId room, WdfRoundSnapshot? round)
		=> round is null ? null : new WdfRoundKey(room, round.Id);

	private static double NormalizeScore(double score)
		=> score <= 1
			? Math.Clamp(score, 0, 1)
			: Math.Clamp(score / 13_333d, 0, 1);
}
