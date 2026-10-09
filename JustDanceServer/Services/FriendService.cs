using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

using System.Text.Json.Nodes;

namespace JustDanceServer.Services;

public sealed record FriendRelationship(Guid ProfileId, string State);

public interface IFriendService
{
	ValueTask<IReadOnlyList<FriendRelationship>> GetRelationshipsAsync(Guid profileId);
	ValueTask<bool> SendInviteAsync(Guid profileId, Guid friendProfileId);
	ValueTask<bool> AcceptInviteAsync(Guid profileId, Guid friendProfileId);
	ValueTask<bool> RemoveRelationshipAsync(Guid profileId, Guid friendProfileId);
}

public sealed class FriendService(
	UserDataContext dbContext,
	IUbiServicesNotificationHub notifications) : IFriendService
{
	public async ValueTask<IReadOnlyList<FriendRelationship>> GetRelationshipsAsync(Guid profileId)
	{
		List<Friendship> friendships = await dbContext.Friendships.AsNoTracking()
			.Where(friendship =>
				friendship.FirstProfileId == profileId || friendship.SecondProfileId == profileId)
			.OrderBy(friendship => friendship.CreatedAt)
			.ToListAsync();

		return [.. friendships.Select(friendship => new FriendRelationship(
			GetOtherProfileId(friendship, profileId),
			GetWireState(friendship, profileId)))];
	}

	public async ValueTask<bool> SendInviteAsync(Guid profileId, Guid friendProfileId)
	{
		if (!await ValidatePairAsync(profileId, friendProfileId))
			return false;

		(Guid first, Guid second) = NormalizePair(profileId, friendProfileId);
		Friendship? friendship = await dbContext.Friendships.FindAsync(first, second);
		bool added = false;
		bool changed = false;
		if (friendship is null)
		{
			friendship = new Friendship
			{
				FirstProfileId = first,
				SecondProfileId = second,
				RequestedByProfileId = profileId,
				State = FriendshipState.Pending
			};
			dbContext.Friendships.Add(friendship);
			added = true;
			changed = true;
		}
		else if (friendship.State == FriendshipState.Pending
			&& friendship.RequestedByProfileId != profileId)
		{
			// Crossing invitations are equivalent to accepting the existing one.
			friendship.State = FriendshipState.Accepted;
			friendship.UpdatedAt = DateTime.UtcNow;
			changed = true;
		}

		try
		{
			await dbContext.SaveChangesAsync();
		}
		catch (DbUpdateException) when (added)
		{
			dbContext.ChangeTracker.Clear();
			friendship = await dbContext.Friendships.FindAsync(first, second);
			if (friendship is null)
				throw;

			if (friendship.State == FriendshipState.Pending
				&& friendship.RequestedByProfileId != profileId)
			{
				friendship.State = FriendshipState.Accepted;
				friendship.UpdatedAt = DateTime.UtcNow;
				await dbContext.SaveChangesAsync();
				changed = true;
			}
		}

		if (changed)
			PublishRelationshipUpdate(profileId, friendProfileId);
		return true;
	}

	public async ValueTask<bool> AcceptInviteAsync(Guid profileId, Guid friendProfileId)
	{
		if (!await ValidatePairAsync(profileId, friendProfileId))
			return false;

		(Guid first, Guid second) = NormalizePair(profileId, friendProfileId);
		Friendship? friendship = await dbContext.Friendships.FindAsync(first, second);
		if (friendship is null)
			return false;
		if (friendship.State == FriendshipState.Accepted)
			return true;
		if (friendship.RequestedByProfileId == profileId)
			return false;

		friendship.State = FriendshipState.Accepted;
		friendship.UpdatedAt = DateTime.UtcNow;

		await dbContext.SaveChangesAsync();
		PublishRelationshipUpdate(profileId, friendProfileId);
		return true;
	}

	public async ValueTask<bool> RemoveRelationshipAsync(Guid profileId, Guid friendProfileId)
	{
		if (profileId == Guid.Empty || friendProfileId == Guid.Empty || profileId == friendProfileId)
			return false;

		(Guid first, Guid second) = NormalizePair(profileId, friendProfileId);
		Friendship? friendship = await dbContext.Friendships.FindAsync(first, second);
		if (friendship is not null)
		{
			dbContext.Friendships.Remove(friendship);
			await dbContext.SaveChangesAsync();
			PublishRelationshipUpdate(profileId, friendProfileId);
		}

		return true;
	}

	private async ValueTask<bool> ValidatePairAsync(Guid profileId, Guid friendProfileId)
	{
		if (profileId == Guid.Empty || friendProfileId == Guid.Empty || profileId == friendProfileId)
			return false;

		return await dbContext.Profiles.CountAsync(profile =>
			profile.Id == profileId || profile.Id == friendProfileId) == 2;
	}

	private static (Guid First, Guid Second) NormalizePair(Guid left, Guid right)
		=> left.CompareTo(right) < 0 ? (left, right) : (right, left);

	private static Guid GetOtherProfileId(Friendship friendship, Guid profileId)
		=> friendship.FirstProfileId == profileId
			? friendship.SecondProfileId
			: friendship.FirstProfileId;

	private static string GetWireState(Friendship friendship, Guid profileId)
	{
		if (friendship.State == FriendshipState.Accepted)
			return "Friends";

		return friendship.RequestedByProfileId == profileId
			? "PendingSentInvite"
			: "PendingReceivedInvite";
	}

	private void PublishRelationshipUpdate(Guid profileId, Guid friendProfileId)
		=> notifications.Publish(
			[profileId, friendProfileId],
			new UbiServicesNotification(
				"FRIENDS_RELATIONSHIP_UPDATE",
				new JsonObject(),
				DateTime.UtcNow));
}
