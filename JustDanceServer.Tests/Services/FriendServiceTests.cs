using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Tests.Services;

public sealed class FriendServiceTests
{
	[Fact]
	public async Task InvitationAcceptanceAndRemovalAreSharedByBothPlayers()
	{
		await using UserDataContext context = CreateContext();
		Profile jduPlayer = CreateProfile("JDU Player");
		Profile jdPlusPlayer = CreateProfile("JD+ Player");
		context.Profiles.AddRange(jduPlayer, jdPlusPlayer);
		await context.SaveChangesAsync();

		FriendService service = new(context, new UbiServicesNotificationHub());
		Assert.True(await service.SendInviteAsync(jduPlayer.Id, jdPlusPlayer.Id));

		FriendRelationship sent = Assert.Single(
			await service.GetRelationshipsAsync(jduPlayer.Id));
		FriendRelationship received = Assert.Single(
			await service.GetRelationshipsAsync(jdPlusPlayer.Id));
		Assert.Equal("PendingSentInvite", sent.State);
		Assert.Equal("PendingReceivedInvite", received.State);

		Assert.True(await service.AcceptInviteAsync(jdPlusPlayer.Id, jduPlayer.Id));
		Assert.Equal("Friends", Assert.Single(
			await service.GetRelationshipsAsync(jduPlayer.Id)).State);
		Assert.Equal("Friends", Assert.Single(
			await service.GetRelationshipsAsync(jdPlusPlayer.Id)).State);

		Assert.True(await service.RemoveRelationshipAsync(jduPlayer.Id, jdPlusPlayer.Id));
		Assert.Empty(await service.GetRelationshipsAsync(jduPlayer.Id));
		Assert.Empty(await service.GetRelationshipsAsync(jdPlusPlayer.Id));
	}

	[Fact]
	public async Task CrossingInvitationsBecomeFriends()
	{
		await using UserDataContext context = CreateContext();
		Profile first = CreateProfile("First");
		Profile second = CreateProfile("Second");
		context.Profiles.AddRange(first, second);
		await context.SaveChangesAsync();

		FriendService service = new(context, new UbiServicesNotificationHub());
		Assert.True(await service.SendInviteAsync(first.Id, second.Id));
		Assert.True(await service.SendInviteAsync(second.Id, first.Id));

		Assert.Equal("Friends", Assert.Single(
			await service.GetRelationshipsAsync(first.Id)).State);
	}

	private static UserDataContext CreateContext()
	{
		DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		return new UserDataContext(options);
	}

	private static Profile CreateProfile(string name)
	{
		Guid id = Guid.NewGuid();
		return new Profile
		{
			Id = id,
			Dancercard = new DancerCard { Id = id, Name = name }
		};
	}
}
