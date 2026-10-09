using JustDanceServer.Configuration;
using JustDanceServer.Services;

using System.Text.Json.Nodes;

namespace JustDanceServer.Tests.Services;

public sealed class UbiServicesNotificationHubTests
{
	[Fact]
	public void PublishFansOutToEveryConnectionForTheProfile()
	{
		UbiServicesNotificationHub hub = new();
		Guid profileId = Guid.NewGuid();
		using UbiServicesNotificationSubscription first = hub.Subscribe(profileId, [], []);
		using UbiServicesNotificationSubscription second = hub.Subscribe(profileId, [], []);
		UbiServicesNotification notification = new(
			"FRIENDS_RELATIONSHIP_UPDATE",
			new JsonObject(),
			DateTime.UtcNow);

		hub.Publish([profileId], notification);

		Assert.Same(notification, Read(first));
		Assert.Same(notification, Read(second));
	}

	[Fact]
	public void SubscriptionFiltersNotificationTypeAndSpace()
	{
		UbiServicesNotificationHub hub = new();
		Guid profileId = Guid.NewGuid();
		using UbiServicesNotificationSubscription subscription = hub.Subscribe(
			profileId,
			["PARTY_MEMBER_ADDED"],
			[JdNextConstants.GameSpaceId]);

		hub.Publish([profileId], new(
			"FRIENDS_RELATIONSHIP_UPDATE",
			new JsonObject(),
			DateTime.UtcNow));
		hub.Publish([profileId], new(
			"PARTY_MEMBER_ADDED",
			new JsonObject(),
			DateTime.UtcNow,
			Guid.NewGuid()));
		UbiServicesNotification expected = new(
			"PARTY_MEMBER_ADDED",
			new JsonObject(),
			DateTime.UtcNow,
			JdNextConstants.GameSpaceId);
		hub.Publish([profileId], expected);

		Assert.Same(expected, Read(subscription));
		Assert.False(subscription.Notifications.TryRead(out _));
	}

	[Fact]
	public void PartyServicePublishesTheSourceDefinedMemberAddedShape()
	{
		UbiServicesNotificationHub hub = new();
		PartyService parties = new(hub);
		Guid owner = Guid.NewGuid();
		Guid member = Guid.NewGuid();
		PartySnapshot party = parties.Create(owner);
		using UbiServicesNotificationSubscription subscription = hub.Subscribe(
			member,
			["PARTY_MEMBER_ADDED"],
			[JdNextConstants.GameSpaceId]);
		PartyAccessToken token = parties.IssueAccessToken(party.PartyId, owner, member)!;

		Assert.True(parties.Join(party.PartyId, member, token.Value));

		UbiServicesNotification notification = Read(subscription);
		Assert.Equal("PARTY_MEMBER_ADDED", notification.NotificationType);
		JsonObject envelope = Assert.IsType<JsonObject>(notification.Content);
		Assert.Equal(JdNextConstants.GameSpaceId, envelope["spaceId"]!.GetValue<Guid>());
		JsonObject content = Assert.IsType<JsonObject>(envelope["content"]);
		Assert.Equal(party.PartyId, content["partyId"]!.GetValue<Guid>());
		Assert.Equal(JdNextConstants.PartyGroupId, content["groupId"]!.GetValue<Guid>());
		Assert.Equal(member, content["profileId"]!.GetValue<Guid>());
		Assert.NotNull(content["addedAt"]);
	}

	private static UbiServicesNotification Read(UbiServicesNotificationSubscription subscription)
	{
		Assert.True(subscription.Notifications.TryRead(out UbiServicesNotification? notification));
		return notification;
	}
}
