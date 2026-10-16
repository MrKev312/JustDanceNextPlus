using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace JustDanceServer.Services;

public sealed record SavedDancerCardCollection(SavedDancerCardDetails Primary, IReadOnlyList<SavedDancerCardDetails> Cards);
public enum DeleteDancerCardResult { Deleted, NotFound, PrimaryCard }

public interface ISavedDancerCardService
{
    Task<SavedDancerCardCollection?> GetAsync(Guid profileId);
    Task<IReadOnlyDictionary<Guid, SavedDancerCardDetails>> GetPublicAsync(IReadOnlyCollection<Guid> profileIds);
    Task<SavedDancerCardDetails?> CreateAsync(Guid profileId, SavedDancerCardInput input);
    Task<SavedDancerCardDetails?> UpdateAsync(Guid profileId, Guid cardId, SavedDancerCardInput input);
    Task<DeleteDancerCardResult> DeleteAsync(Guid profileId, Guid cardId);
}

public sealed class SavedDancerCardService(UserDataContext db, TimeProvider timeProvider) : ISavedDancerCardService
{
    public async Task<IReadOnlyDictionary<Guid, SavedDancerCardDetails>> GetPublicAsync(IReadOnlyCollection<Guid> profileIds)
    {
        Guid[] ids = [.. profileIds.Distinct()];
        var profiles = await db.Profiles.AsNoTracking().Include(profile => profile.Dancercard)
            .Where(profile => ids.Contains(profile.Id)).ToListAsync();
        Guid[] primaryIds = [.. profiles.Select(profile => profile.Dancercard.Id)];
        var saved = await db.SavedDancerCards.AsNoTracking()
            .Where(card => ids.Contains(card.ProfileId) && primaryIds.Contains(card.Id)).ToDictionaryAsync(card => card.Id);
        // Public lookups expose only the legacy primary, never a player's additional cards.
        // Unknown historical dates are represented by the epoch; reading another profile does not create state.
        return profiles.ToDictionary(profile => profile.Id, profile => SavedDancerCardDetails.From(profile.Dancercard,
            saved.GetValueOrDefault(profile.Dancercard.Id) ?? new SavedDancerCard
            {
                Id = profile.Dancercard.Id,
                CreatedAt = DateTime.UnixEpoch,
                UpdatedAt = DateTime.UnixEpoch
            }));
    }

    public async Task<SavedDancerCardCollection?> GetAsync(Guid profileId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        Profile? profile = await FindProfileAsync(profileId);
        if (profile is null)
			return null;
        SavedDancerCard primary = await EnsurePrimaryAsync(profile);
        List<SavedDancerCard> cards = await db.SavedDancerCards.Where(card => card.ProfileId == profileId)
            .OrderBy(card => card.CreatedAt).ThenBy(card => card.Id).ToListAsync();
        SavedDancerCardDetails primaryDetails = SavedDancerCardDetails.From(profile.Dancercard, primary);
        var details = cards.Where(card => card.Id != primary.Id)
            .Select(card => SavedDancerCardDetails.From(ReadCosmetics(card), card));
        await transaction.CommitAsync();
        return new(primaryDetails, [primaryDetails, .. details]);
	}

	public async Task<SavedDancerCardDetails?> CreateAsync(Guid profileId, SavedDancerCardInput input)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        Profile? profile = await FindProfileAsync(profileId);
        if (profile is null)
			return null;
        await EnsurePrimaryAsync(profile);
        DancerCard cosmetics = new();
        input.ApplyTo(cosmetics);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        SavedDancerCard saved = new()
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            CreatedAt = now,
            UpdatedAt = now,
            CosmeticsJson = JsonSerializer.Serialize(cosmetics)
        };
        db.SavedDancerCards.Add(saved);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return SavedDancerCardDetails.From(cosmetics, saved);
	}

	public async Task<SavedDancerCardDetails?> UpdateAsync(Guid profileId, Guid cardId, SavedDancerCardInput input)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        Profile? profile = await FindProfileAsync(profileId);
        if (profile is null)
			return null;
        bool isPrimary = cardId == profile.Dancercard.Id;
        SavedDancerCard? saved = isPrimary ? await EnsurePrimaryAsync(profile)
            : await db.SavedDancerCards.SingleOrDefaultAsync(card => card.ProfileId == profileId && card.Id == cardId);
        if (saved is null)
			return null;
        DancerCard cosmetics = isPrimary ? profile.Dancercard : ReadCosmetics(saved);
        input.ApplyTo(cosmetics);
        saved.CosmeticsJson = JsonSerializer.Serialize(cosmetics);
        saved.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return SavedDancerCardDetails.From(cosmetics, saved);
	}

	public async Task<DeleteDancerCardResult> DeleteAsync(Guid profileId, Guid cardId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        Profile? profile = await FindProfileAsync(profileId);
        if (profile is null)
			return DeleteDancerCardResult.NotFound;
        // Only additional cards are deleted in the capture. Never orphan the legacy primary card.
        if (profile.Dancercard.Id == cardId)
			return DeleteDancerCardResult.PrimaryCard;
        SavedDancerCard? saved = await db.SavedDancerCards
            .SingleOrDefaultAsync(card => card.ProfileId == profileId && card.Id == cardId);
        if (saved is null)
			return DeleteDancerCardResult.NotFound;
        db.SavedDancerCards.Remove(saved);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return DeleteDancerCardResult.Deleted;
	}

	private Task<Profile?> FindProfileAsync(Guid id)
        => db.Profiles.Include(profile => profile.Dancercard).SingleOrDefaultAsync(profile => profile.Id == id);

    private async Task<SavedDancerCard> EnsurePrimaryAsync(Profile profile)
    {
        SavedDancerCard? saved = await db.SavedDancerCards.SingleOrDefaultAsync(card => card.Id == profile.Dancercard.Id);
        if (saved is not null)
        {
            if (saved.ProfileId != profile.Id)
				throw new InvalidOperationException("Dancer card ownership mismatch.");
            return saved;
		}

		// Original creation dates were not stored. Start tracking when the new API is first used.
		DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        saved = new()
        {
            Id = profile.Dancercard.Id,
            ProfileId = profile.Id,
            CreatedAt = now,
            UpdatedAt = now,
            CosmeticsJson = JsonSerializer.Serialize(profile.Dancercard)
        };
        db.SavedDancerCards.Add(saved);
        await db.SaveChangesAsync();
        return saved;
	}

	private static DancerCard ReadCosmetics(SavedDancerCard card)
        => JsonSerializer.Deserialize<DancerCard>(card.CosmeticsJson)
            ?? throw new InvalidOperationException("Invalid saved dancer card.");
}
