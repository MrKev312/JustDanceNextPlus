using System.Globalization;

namespace JustDanceServer.Services;

public sealed record WdfSessionSummary(
    int UniquePlayerCount,
    IReadOnlyList<string> Countries);

internal sealed class WdfSessionTracker(TimeSpan activePlayerTimeout)
{
    private readonly Dictionary<Guid, WdfMembership> activePlayers = [];
    private readonly Dictionary<Guid, WdfVisit> visits = [];
    private readonly Dictionary<Guid, string> playerCountries = [];
    private static readonly TimeSpan RecapRetention = TimeSpan.FromDays(1);
    private DateTimeOffset nextHistoryCleanup;

    public void Begin(WdfRoomId room, Guid playerId, DateTimeOffset now)
    {
        RemoveInactive(now);
        if (!activePlayers.ContainsKey(playerId))
            visits[playerId] = new WdfVisit();

        Touch(room, playerId, now);
    }

    public void Touch(WdfRoomId room, Guid playerId, DateTimeOffset now)
    {
        RemoveInactive(now);
        visits.TryAdd(playerId, new WdfVisit());
        visits[playerId].LastSeen = now;

        foreach ((Guid otherPlayerId, WdfMembership membership) in activePlayers)
        {
            if (otherPlayerId == playerId || membership.Room != room)
                continue;

            visits[playerId].EncounteredPlayers.Add(otherPlayerId);
            visits.TryAdd(otherPlayerId, new WdfVisit());
            visits[otherPlayerId].EncounteredPlayers.Add(playerId);
        }

        activePlayers[playerId] = new WdfMembership(room, now);
    }

    public void Leave(WdfRoomId room, Guid playerId)
    {
        if (activePlayers.TryGetValue(playerId, out WdfMembership? membership)
            && membership.Room == room)
        {
            activePlayers.Remove(playerId);
        }
    }

    public void SetCountry(Guid playerId, int countryId)
        => playerCountries[playerId] = countryId.ToString(CultureInfo.InvariantCulture);

    public int GetActivePlayerCount(WdfRoomId room, DateTimeOffset now)
    {
        RemoveInactive(now);
        return activePlayers.Values.Count(membership => membership.Room == room);
    }

    public WdfSessionSummary GetSummary(Guid playerId, DateTimeOffset now)
    {
        RemoveInactive(now);
        if (playerId == Guid.Empty || !visits.TryGetValue(playerId, out WdfVisit? visit))
            return new WdfSessionSummary(0, []);

        string[] countries = [.. visit.EncounteredPlayers
            .Select(encounteredPlayerId => playerCountries.GetValueOrDefault(encounteredPlayerId, "0"))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        return new WdfSessionSummary(visit.EncounteredPlayers.Count, countries);
    }

    private void RemoveInactive(DateTimeOffset now)
    {
        DateTimeOffset cutoff = now - activePlayerTimeout;
        foreach (Guid playerId in activePlayers
            .Where(pair => pair.Value.LastSeen < cutoff)
            .Select(pair => pair.Key)
            .ToArray())
        {
            activePlayers.Remove(playerId);
        }

        if (now < nextHistoryCleanup)
            return;
        nextHistoryCleanup = now.AddMinutes(1);
        foreach (Guid playerId in visits.Where(pair => pair.Value.LastSeen < now - RecapRetention
            && !activePlayers.ContainsKey(pair.Key)).Select(pair => pair.Key).ToArray())
            visits.Remove(playerId);
        HashSet<Guid> referencedPlayers = [.. visits.Keys, .. visits.Values.SelectMany(visit => visit.EncounteredPlayers)];
        foreach (Guid playerId in playerCountries.Keys.Where(id => !referencedPlayers.Contains(id)).ToArray())
            playerCountries.Remove(playerId);
    }

    private sealed record WdfMembership(WdfRoomId Room, DateTimeOffset LastSeen);

    private sealed class WdfVisit
    {
        public DateTimeOffset LastSeen { get; set; }
        public HashSet<Guid> EncounteredPlayers { get; } = [];
    }
}
