namespace JustDanceServer.Services;

public interface IJduRecommendationService
{
    ValueTask<string[]> GetOrderedMapsAsync(Guid? profileId = null);
    ValueTask<IReadOnlyDictionary<string, string[]>> GetRelatedSongsAsync();
}

public sealed class JduRecommendationService(IRecommendationService recommendationService) : IJduRecommendationService
{
    public async ValueTask<string[]> GetOrderedMapsAsync(Guid? profileId = null)
    {
        string[] popular = await recommendationService.GetMapsAsync(new RecommendationQuery(RecommendationPolicy.Popular, Count: null));
        if (!profileId.HasValue)
            return popular;
        string[] personal = await recommendationService.GetMapsAsync(new RecommendationQuery(RecommendationPolicy.ForYou, profileId));
        // Sorting endpoints still include the complete catalog, including alternate routines.
        return [.. personal.Concat(popular).Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    public ValueTask<IReadOnlyDictionary<string, string[]>> GetRelatedSongsAsync()
        => recommendationService.GetRelatedSongsAsync();
}
