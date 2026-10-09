using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JustDanceServer.Services;

public sealed record MapCompletion(
	Guid ProfileId, string MapName, int Score, string Platform,
	bool Cooperative, HighscorePerformance Performance, bool GrantXp);

public sealed record MapCompletionResult(ScoreUpdateResult Score, PlayerProgression? Progression);

public interface IMapCompletionService
{
	ValueTask<MapCompletionResult> RecordAsync(MapCompletion completion);
}

public sealed class MapCompletionService(
	UserDataContext dbContext,
	IScoreService scores,
	IProgressionService progression,
	IPlayerActivityService activity) : IMapCompletionService
{
	public async ValueTask<MapCompletionResult> RecordAsync(MapCompletion completion)
	{
		if (completion.ProfileId == Guid.Empty || string.IsNullOrWhiteSpace(completion.MapName)
			|| completion.Score is < 0 or > 13333)
			return new(new(false, false), null);
		if (completion.Score == 0)
			return new(new(true, false), null);

		await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
			? await dbContext.Database.BeginTransactionAsync() : null;
		ScoreUpdateResult result = await scores.RecordMapPlayAsync(completion.Performance, completion.Score,
			completion.ProfileId, completion.MapName, completion.Platform, completion.Cooperative);
		if (!result.Success)
			return new(result, null);

		await activity.RecordJdNextObservationAsync(completion.ProfileId, completion.MapName,
			completion.Score, completion.Platform, completion.Cooperative);
		PlayerProgression? updated = completion.GrantXp
			? await progression.RecordMapCompletionAsync(completion.ProfileId, ProgressionFamily.JDPlus, completion.Score)
			: null;
		if (transaction is not null)
			await transaction.CommitAsync();
		return new(result, updated);
	}
}
