using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading.Channels;

namespace JustDanceServer.Services;

public sealed record UbiServicesNotification(
	string NotificationType,
	JsonNode Content,
	DateTime DatePosted,
	Guid? SpaceId = null);

public interface IUbiServicesNotificationHub
{
	UbiServicesNotificationSubscription Subscribe(
		Guid profileId,
		IEnumerable<string> notificationTypes,
		IEnumerable<Guid> spaceIds);

	void Publish(IEnumerable<Guid> profileIds, UbiServicesNotification notification);
}

public sealed class UbiServicesNotificationSubscription : IDisposable
{
	private readonly Action dispose;
	private int isDisposed;

	internal UbiServicesNotificationSubscription(
		ChannelReader<UbiServicesNotification> notifications,
		Action dispose)
	{
		Notifications = notifications;
		this.dispose = dispose;
	}

	public ChannelReader<UbiServicesNotification> Notifications { get; }

	public void Dispose()
	{
		if (Interlocked.Exchange(ref isDisposed, 1) == 0)
			dispose();
	}
}

public sealed class UbiServicesNotificationHub : IUbiServicesNotificationHub
{
	private readonly Lock subscriptionLock = new();
	private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Subscriber>>
		subscribersByProfile = [];

	public UbiServicesNotificationSubscription Subscribe(
		Guid profileId,
		IEnumerable<string> notificationTypes,
		IEnumerable<Guid> spaceIds)
	{
		Guid subscriptionId = Guid.NewGuid();
		Channel<UbiServicesNotification> channel = Channel.CreateBounded<UbiServicesNotification>(
			new BoundedChannelOptions(256)
			{
				SingleReader = true,
				SingleWriter = false,
				FullMode = BoundedChannelFullMode.DropOldest
			});
		Subscriber subscriber = new(
			channel,
			new HashSet<string>(notificationTypes, StringComparer.OrdinalIgnoreCase),
			new HashSet<Guid>(spaceIds));
		// Adding a connection and removing an empty profile must be atomic together:
		// otherwise reconnecting can add to a dictionary that has just been detached.
		lock (subscriptionLock)
		{
			ConcurrentDictionary<Guid, Subscriber> profileSubscribers =
				subscribersByProfile.GetOrAdd(profileId, _ => []);
			profileSubscribers[subscriptionId] = subscriber;
		}

		return new UbiServicesNotificationSubscription(
			channel.Reader,
			() => Unsubscribe(profileId, subscriptionId, subscriber));
	}

	public void Publish(IEnumerable<Guid> profileIds, UbiServicesNotification notification)
	{
		foreach (Guid profileId in profileIds.Distinct())
		{
			if (!subscribersByProfile.TryGetValue(profileId, out ConcurrentDictionary<Guid, Subscriber>? subscribers))
				continue;

			foreach (Subscriber subscriber in subscribers.Values)
			{
				if (subscriber.Accepts(notification))
					subscriber.Channel.Writer.TryWrite(notification);
			}
		}
	}

	private void Unsubscribe(Guid profileId, Guid subscriptionId, Subscriber subscriber)
	{
		subscriber.Channel.Writer.TryComplete();
		lock (subscriptionLock)
		{
			if (!subscribersByProfile.TryGetValue(profileId, out ConcurrentDictionary<Guid, Subscriber>? subscribers))
				return;

			subscribers.TryRemove(subscriptionId, out _);
			if (subscribers.IsEmpty)
				subscribersByProfile.TryRemove(
					new KeyValuePair<Guid, ConcurrentDictionary<Guid, Subscriber>>(
						profileId,
						subscribers));
		}
	}

	private sealed record Subscriber(
		Channel<UbiServicesNotification> Channel,
		IReadOnlySet<string> NotificationTypes,
		IReadOnlySet<Guid> SpaceIds)
	{
		public bool Accepts(UbiServicesNotification notification)
			=> (NotificationTypes.Count == 0
					|| NotificationTypes.Contains(notification.NotificationType))
				&& (notification.SpaceId is not Guid spaceId
					|| SpaceIds.Count == 0
					|| SpaceIds.Contains(spaceId));
	}
}
