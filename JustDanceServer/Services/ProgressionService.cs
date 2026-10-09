using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Services;

public sealed record ProgressionSnapshot(
	int Xp,
	int Level,
	int Prestige,
	IReadOnlySet<string> Unlocks,
	int GachaRank,
	int GachaPullCount);

public interface IProgressionService
{
	ValueTask<ProgressionSnapshot> GetAsync(Guid profileId, ProgressionFamily family);
	ValueTask<PlayerProgression> SetProgressionAsync(
		Guid profileId,
		ProgressionFamily family,
		int xp,
		int level,
		int prestige);
	ValueTask<PlayerProgression> RecordMapCompletionAsync(
		Guid profileId,
		ProgressionFamily family,
		int score);
	ValueTask GrantUnlocksAsync(
		Guid profileId,
		ProgressionFamily family,
		IEnumerable<string> unlockKeys,
		string source);
	ValueTask<IReadOnlyList<PlayerQuestProgress>> GetQuestProgressAsync(
		Guid profileId,
		ProgressionFamily family);
	ValueTask<PlayerQuestProgress> SetQuestProgressAsync(
		Guid profileId,
		ProgressionFamily family,
		string questKey,
		int progress,
		bool completed,
		bool claimed);
	ValueTask<PlayerGachaState> SetGachaStateAsync(
		Guid profileId,
		ProgressionFamily family,
		int rank,
		int pullCount);
}

public sealed class ProgressionService(UserDataContext dbContext) : IProgressionService
{
	public async ValueTask<ProgressionSnapshot> GetAsync(
		Guid profileId,
		ProgressionFamily family)
	{
		PlayerProgression? progression = await dbContext.PlayerProgressions.AsNoTracking()
			.FirstOrDefaultAsync(value => value.ProfileId == profileId && value.Family == family) ?? await CreateDefaultAsync(profileId, family, false);
		HashSet<string> unlocks = [.. await dbContext.PlayerUnlocks.AsNoTracking()
			.Where(value => value.ProfileId == profileId && value.Family == family)
			.Select(value => value.UnlockKey)
			.ToListAsync()];
		PlayerGachaState? gacha = await dbContext.PlayerGachaStates.AsNoTracking()
			.Where(value => value.ProfileId == profileId && value.Family == family)
			.Select(value => new PlayerGachaState
			{
				Rank = value.Rank,
				PullCount = value.PullCount
			})
			.FirstOrDefaultAsync();

		return new ProgressionSnapshot(
			progression.Xp,
			progression.Level,
			progression.Prestige,
			unlocks,
			gacha?.Rank ?? 0,
			gacha?.PullCount ?? 0);
	}

	public async ValueTask<PlayerProgression> SetProgressionAsync(
		Guid profileId,
		ProgressionFamily family,
		int xp,
		int level,
		int prestige)
	{
		PlayerProgression progression = await GetOrCreateAsync(profileId, family);
		progression.Xp = Math.Max(0, xp);
		progression.Level = Math.Max(1, level);
		progression.Prestige = Math.Max(0, prestige);
		progression.UpdatedAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync();
		return progression;
	}

	public async ValueTask<PlayerProgression> RecordMapCompletionAsync(
		Guid profileId,
		ProgressionFamily family,
		int score)
	{
		PlayerProgression progression = await GetOrCreateAsync(profileId, family);
		int earnedXp = Math.Clamp(score, 0, 13_333) / 100;
		progression.Xp += earnedXp;
		progression.UpdatedAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync();
		return progression;
	}

	public async ValueTask GrantUnlocksAsync(
		Guid profileId,
		ProgressionFamily family,
		IEnumerable<string> unlockKeys,
		string source)
	{
		string[] normalized = [.. unlockKeys
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)];
		if (normalized.Length == 0)
			return;

		HashSet<string> existing = new(
			await dbContext.PlayerUnlocks
			.Where(value => value.ProfileId == profileId && value.Family == family)
			.Select(value => value.UnlockKey)
			.ToListAsync(),
			StringComparer.OrdinalIgnoreCase);
		foreach (string unlockKey in normalized.Where(value => !existing.Contains(value)))
		{
			dbContext.PlayerUnlocks.Add(new PlayerUnlock
			{
				ProfileId = profileId,
				Family = family,
				UnlockKey = unlockKey,
				Source = source
			});
		}

		await dbContext.SaveChangesAsync();
	}

	public async ValueTask<IReadOnlyList<PlayerQuestProgress>> GetQuestProgressAsync(
		Guid profileId,
		ProgressionFamily family)
		=> await dbContext.PlayerQuestProgress.AsNoTracking()
			.Where(value => value.ProfileId == profileId && value.Family == family)
			.OrderBy(value => value.QuestKey)
			.ToListAsync();

	public async ValueTask<PlayerQuestProgress> SetQuestProgressAsync(
		Guid profileId,
		ProgressionFamily family,
		string questKey,
		int progress,
		bool completed,
		bool claimed)
	{
		questKey = questKey.Trim();
		if (questKey.Length == 0)
			throw new ArgumentException("A quest key is required.", nameof(questKey));

		PlayerQuestProgress? state = await dbContext.PlayerQuestProgress.FirstOrDefaultAsync(
			value => value.ProfileId == profileId
				&& value.Family == family
				&& value.QuestKey == questKey);
		if (state is null)
		{
			state = new PlayerQuestProgress
			{
				ProfileId = profileId,
				Family = family,
				QuestKey = questKey
			};
			dbContext.PlayerQuestProgress.Add(state);
		}

		DateTime now = DateTime.UtcNow;
		state.Progress = Math.Max(0, progress);
		state.CompletedAt = completed ? state.CompletedAt ?? now : null;
		state.ClaimedAt = completed && claimed ? state.ClaimedAt ?? now : null;
		state.UpdatedAt = now;
		await dbContext.SaveChangesAsync();
		return state;
	}

	public async ValueTask<PlayerGachaState> SetGachaStateAsync(
		Guid profileId,
		ProgressionFamily family,
		int rank,
		int pullCount)
	{
		PlayerGachaState? state = await dbContext.PlayerGachaStates
			.FirstOrDefaultAsync(value => value.ProfileId == profileId && value.Family == family);
		if (state is null)
		{
			state = new PlayerGachaState { ProfileId = profileId, Family = family };
			dbContext.PlayerGachaStates.Add(state);
		}

		state.Rank = Math.Max(0, rank);
		state.PullCount = Math.Max(0, pullCount);
		state.UpdatedAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync();
		return state;
	}

	private async ValueTask<PlayerProgression> GetOrCreateAsync(
		Guid profileId,
		ProgressionFamily family)
		=> await dbContext.PlayerProgressions
			.FirstOrDefaultAsync(value => value.ProfileId == profileId && value.Family == family)
			?? await CreateDefaultAsync(profileId, family, true);

	private async ValueTask<PlayerProgression> CreateDefaultAsync(
		Guid profileId,
		ProgressionFamily family,
		bool track)
	{
		Profile? profile = await dbContext.Profiles.AsNoTracking()
			.FirstOrDefaultAsync(value => value.Id == profileId)
			?? throw new InvalidOperationException($"Profile '{profileId}' does not exist.");
		PlayerProgression progression = new()
		{
			ProfileId = profileId,
			Family = family,
			Xp = family == ProgressionFamily.JDPlus ? profile.CurrentXP : 1,
			Level = family == ProgressionFamily.JDPlus ? profile.CurrentLevel : 1,
			Prestige = family == ProgressionFamily.JDPlus ? profile.PrestigeGrade : 0
		};
		if (track)
			dbContext.PlayerProgressions.Add(progression);
		return progression;
	}
}
