using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public sealed class WdfDirector(
    IMapService mapService,
    WdfCatalog catalog,
    TimeProvider timeProvider,
    ILogger<WdfDirector> logger) : BackgroundService, IWdfCommandService
{
    private const double VoteChoiceDurationSeconds = 20;
    private const double VoteResultDelaySeconds = 30;
    private const double VoteDurationSeconds = 51;
    private const double LobbyDurationSeconds = 15;
    private const double WaitingDurationSeconds = 20;
    private const double RecapDurationSeconds = 20;
    private const double RecapFetchLeadSeconds = 7;
    private const double DefaultMapDurationSeconds = 180;

    private readonly Lock stateLock = new();
    private readonly Dictionary<WdfRoomId, WdfStandardRoomRuntime> states = [];

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Advance();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1), timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                Advance();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    internal void Advance()
    {
        lock (stateLock)
        {
            double now = GetServerTime();
            foreach (WdfRoomId room in WdfRoomId.All)
                GetCurrentRoundLocked(room, GetStateLocked(room), now);
        }
    }

    public WdfCurrentMap? GetCurrentMap(WdfRoomId room)
    {
        WdfRoundSnapshot? round = GetCurrentRound(room);
        return round?.MapName is string mapName
            ? new WdfCurrentMap(mapName, round.GameplayStartTime, round.GameplayEndTime)
            : null;
    }

    internal WdfRoundSnapshot? GetCurrentRound(WdfRoomId room)
    {
        lock (stateLock)
        {
            double now = GetServerTime();
            return ToSnapshot(GetCurrentRoundLocked(room, GetStateLocked(room), now), now);
        }
    }

    public WdfScreenList GetScreens(WdfRoomId room, double? startTime)
    {
        lock (stateLock)
        {
            double now = GetServerTime();
            WdfStandardRoomRuntime state = GetStateLocked(room);
            WdfRound? current = GetCurrentRoundLocked(room, state, now);
            if (current is null)
                return new WdfScreenList();

            double requestedStart = startTime is double value && double.IsFinite(value)
                ? Math.Clamp(value, now - 300, now + 3600)
                : now;
            double effectiveStart = Math.Max(requestedStart, now);
            return new WdfScreenList
            {
                Screens = [.. state.Rounds
                    .SelectMany(CreateScreens)
                    .Where(screen => screen.EndTime > effectiveStart)
                    .Take(6)]
            };
        }
    }

    public bool SubmitVote(WdfRoomId room, Guid playerId, string mapName)
    {
        if (playerId == Guid.Empty || string.IsNullOrWhiteSpace(mapName))
            return false;

        lock (stateLock)
        {
            double now = GetServerTime();
            WdfRound? round = GetCurrentRoundLocked(room, GetStateLocked(room), now);
            if (round is null)
                return false;

            double voteResultFetchTime = round.StartTime + VoteResultDelaySeconds;
            if (round.IsResolved || now >= voteResultFetchTime)
                return false;

            string? option = round.VoteOptions.FirstOrDefault(candidate =>
                candidate.Equals(mapName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (option is null)
                return false;

            round.Votes[playerId] = option;
            return true;
        }
    }

    public WdfVoteResult GetVoteResult(WdfRoomId room)
    {
        lock (stateLock)
        {
            double now = GetServerTime();
            WdfStandardRoomRuntime state = GetStateLocked(room);
            WdfRound? round = GetCurrentRoundLocked(room, state, now);
            if (round is null)
                return new WdfVoteResult();
            if (!round.IsResolved && now >= round.StartTime + VoteChoiceDurationSeconds)
            {
                ResolveRoundLocked(room, state, round);
                GetCurrentRoundLocked(room, state, now);
            }

            IReadOnlyDictionary<string, int> counts = round.VoteResult
                ?? ToPercentages(CountVotes(round), null);
            IEnumerable<string> orderedOptions = round.MapName is string winner
                ? round.VoteOptions.OrderByDescending(mapName =>
                    mapName.Equals(winner, StringComparison.OrdinalIgnoreCase))
                : round.VoteOptions;
            return new WdfVoteResult
            {
                Entries = [.. orderedOptions.Select(mapName => new WdfVoteResultEntry
                {
                    Name = mapName,
                    Value = counts.GetValueOrDefault(mapName)
                })]
            };
        }
    }

    public void Enqueue(WdfRoomId room, string mapName) => EnqueueSet(room, [mapName]);

    public void EnqueueSet(WdfRoomId room, IEnumerable<string> mapNames)
    {
        WdfQueueItem item = catalog.ValidateQueueItem(room, mapNames, nameof(mapNames));
        lock (stateLock)
            GetStateLocked(room).RequestedQueue.Enqueue(item);
    }

    public void ReplaceQueue(WdfRoomId room, IEnumerable<string> mapNames)
        => ReplaceQueueSets(room, mapNames.Select(mapName => (IEnumerable<string>)[mapName]));

    public void ReplaceQueueSets(WdfRoomId room, IEnumerable<IEnumerable<string>> mapSets)
    {
        WdfQueueItem[] items = [.. mapSets.Select(mapNames =>
            catalog.ValidateQueueItem(room, mapNames, nameof(mapSets)))];
        lock (stateLock)
        {
            WdfStandardRoomRuntime state = GetStateLocked(room);
            state.RequestedQueue.Clear();
            foreach (WdfQueueItem item in items)
                state.RequestedQueue.Enqueue(item);
            RemoveUnstartedRoundsLocked(state);
        }
    }

    public IReadOnlyList<string> GetQueue(WdfRoomId room)
    {
        lock (stateLock)
            return [.. GetStateLocked(room).RequestedQueue.SelectMany(item => item.Candidates)];
    }

    public IReadOnlyList<IReadOnlyList<string>> GetQueueSets(WdfRoomId room)
    {
        lock (stateLock)
            return [.. GetStateLocked(room).RequestedQueue
                .Select(item => (IReadOnlyList<string>)[.. item.Candidates])];
    }

    private WdfStandardRoomRuntime GetStateLocked(WdfRoomId room)
    {
        if (!states.TryGetValue(room, out WdfStandardRoomRuntime? state))
        {
            state = new WdfStandardRoomRuntime();
            states[room] = state;
        }

        return state;
    }

    private void RemoveUnstartedRoundsLocked(WdfStandardRoomRuntime state)
    {
        double now = GetServerTime();
        int currentIndex = state.Rounds.FindIndex(round => round.EndTime > now);
        if (currentIndex >= 0 && currentIndex + 1 < state.Rounds.Count)
            state.Rounds.RemoveRange(currentIndex + 1, state.Rounds.Count - currentIndex - 1);
    }

    private WdfRound? GetCurrentRoundLocked(WdfRoomId room, WdfStandardRoomRuntime state, double now)
    {
        EnsureInitializedLocked(room, state, now);
        for (int transitions = 0; transitions < 256 && state.Rounds.Count > 0; transitions++)
        {
            WdfRound? current = state.Rounds.FirstOrDefault(round => round.EndTime > now);
            if (current is null)
            {
                AddRoundLocked(room, state, state.Rounds[^1].EndTime);
                continue;
            }

            if (!current.IsResolved && now >= current.StartTime + VoteResultDelaySeconds)
                ResolveRoundLocked(room, state, current);
            if (current.IsResolved && ReferenceEquals(current, state.Rounds[^1]))
                AddRoundLocked(room, state, current.EndTime);
            if (current.EndTime <= now)
                continue;

            PruneOldRoundsLocked(state, current);
            return current;
        }

        return state.Rounds.FirstOrDefault(round => round.EndTime > now);
    }

    private void EnsureInitializedLocked(WdfRoomId room, WdfStandardRoomRuntime state, double now)
    {
        if (state.Rounds.Count == 0 && mapService.Maps.Count > 0)
            AddRoundLocked(room, state, now);
    }

    private void AddRoundLocked(WdfRoomId room, WdfStandardRoomRuntime state, double startTime)
    {
        string? previousMap = state.Rounds.LastOrDefault(round => round.IsResolved)?.MapName;
        IReadOnlyList<string> voteOptions = SelectVoteOptionsLocked(room, state, previousMap);
        if (voteOptions.Count == 0)
            return;

        state.Rounds.Add(new WdfRound
        {
            Id = ++state.NextRoundId,
            VoteOptions = voteOptions,
            StartTime = startTime,
            GameplayStartTime = startTime + VoteDurationSeconds + LobbyDurationSeconds
        });
    }

    private IReadOnlyList<string> SelectVoteOptionsLocked(
        WdfRoomId room,
        WdfStandardRoomRuntime state,
        string? previousMap)
    {
        if (state.RequestedQueue.TryDequeue(out WdfQueueItem? queued))
            return SelectFromSet(queued.Candidates, previousMap);

        IReadOnlyList<string> eligibleMapNames = catalog.GetMapNames(room);
        int desiredCount = Math.Min(2, eligibleMapNames.Count);
        List<string> selected = [with(desiredCount)];
        while (selected.Count < desiredCount)
        {
            bool avoidPrevious = eligibleMapNames.Count > desiredCount;
            int index = FindShuffleCandidate(state.ShuffleBag, selected, previousMap, avoidPrevious);
            if (index < 0)
            {
                RefillShuffleBag(state, eligibleMapNames);
                index = FindShuffleCandidate(state.ShuffleBag, selected, previousMap, avoidPrevious);
            }

            if (index < 0)
                break;

            selected.Add(state.ShuffleBag[index]);
            state.ShuffleBag.RemoveAt(index);
        }

        return selected;
    }

    private static int FindShuffleCandidate(
        List<string> shuffleBag,
        IReadOnlyCollection<string> selected,
        string? previousMap,
        bool avoidPrevious)
        => shuffleBag.FindIndex(mapName =>
            !selected.Contains(mapName, StringComparer.OrdinalIgnoreCase)
            && (!avoidPrevious || !mapName.Equals(previousMap, StringComparison.OrdinalIgnoreCase)));

    private static void RefillShuffleBag(WdfStandardRoomRuntime state, IReadOnlyList<string> eligibleMapNames)
    {
        string[] mapNames = [.. eligibleMapNames];
        Random.Shared.Shuffle(mapNames);
        state.ShuffleBag.Clear();
        state.ShuffleBag.AddRange(mapNames);
    }

    private static IReadOnlyList<string> SelectFromSet(
        IReadOnlyList<string> candidates,
        string? previousMap)
    {
        string[] shuffled = [.. candidates];
        Random.Shared.Shuffle(shuffled);
        int desiredCount = Math.Min(2, shuffled.Length);
        if (shuffled.Length > desiredCount && previousMap is not null)
        {
            shuffled = [.. shuffled.Where(mapName =>
                !mapName.Equals(previousMap, StringComparison.OrdinalIgnoreCase))];
        }

        return [.. shuffled.Take(desiredCount)];
    }

    private void ResolveRoundLocked(WdfRoomId room, WdfStandardRoomRuntime state, WdfRound round)
    {
        Dictionary<string, int> counts = CountVotes(round);
        int highest = counts.Values.DefaultIfEmpty(0).Max();
        string[] tied = [.. round.VoteOptions.Where(mapName => counts[mapName] == highest)];
        string winner = tied[Random.Shared.Next(tied.Length)];
        ServerMap map = mapService.GetMap(winner)
            ?? throw new InvalidOperationException($"WDF candidate '{winner}' no longer exists.");
        double mapDuration = map.Song.MapLength > 0
            ? Math.Max(30, map.Song.MapLength)
            : DefaultMapDurationSeconds;
        round.MapName = map.Song.MapName;
        round.GameplayEndTime = round.GameplayStartTime + mapDuration;
        round.EndTime = round.GameplayEndTime + WaitingDurationSeconds + RecapDurationSeconds;
        round.VoteResult = ToPercentages(counts, tied.Length > 1 ? winner : null);
        logger.LogInformation("Next WDF song: {MapName}", round.MapName);
    }

    private static Dictionary<string, int> CountVotes(WdfRound round)
    {
        Dictionary<string, int> counts = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (string mapName in round.VoteOptions)
            counts[mapName] = 0;
        foreach (string mapName in round.Votes.Values)
            counts[mapName]++;
        return counts;
    }

    private static Dictionary<string, int> ToPercentages(
        IReadOnlyDictionary<string, int> counts,
        string? tieWinner)
    {
        int total = counts.Values.Sum();
        Dictionary<string, int> percentages = counts.Keys.ToDictionary(
            mapName => mapName,
            mapName => total == 0 ? 0 : (int)Math.Round(counts[mapName] * 100d / total),
            StringComparer.OrdinalIgnoreCase);
        if (tieWinner is null)
            return percentages;

        string? loser = percentages.Keys.FirstOrDefault(mapName =>
            !mapName.Equals(tieWinner, StringComparison.OrdinalIgnoreCase));
        if (loser is null)
        {
            percentages[tieWinner] = 100;
            return percentages;
        }

        if (total == 0)
            percentages[tieWinner] = 100;
        else
        {
            percentages[tieWinner]++;
            percentages[loser]--;
        }

        return percentages;
    }

    private static void PruneOldRoundsLocked(WdfStandardRoomRuntime state, WdfRound current)
    {
        int currentIndex = state.Rounds.IndexOf(current);
        if (currentIndex > 2)
            state.Rounds.RemoveRange(0, currentIndex - 2);
    }

    private static WdfRoundSnapshot? ToSnapshot(WdfRound? round, double now)
        => round is null
            ? null
            : new WdfRoundSnapshot(
                round.Id,
                round.MapName,
                round.GameplayStartTime,
                round.GameplayEndTime,
                GetPhase(round, now));

    private static WdfPhase GetPhase(WdfRound round, double now)
    {
        if (now < round.StartTime + VoteDurationSeconds)
            return WdfPhase.Vote;
        if (now < round.GameplayStartTime)
            return WdfPhase.VoteLobby;
        if (now < round.GameplayEndTime)
            return WdfPhase.InGame;
        if (now < round.GameplayEndTime + WaitingDurationSeconds)
            return WdfPhase.Waiting;
        return WdfPhase.Recap;
    }

    private static IReadOnlyList<WdfScreen> CreateScreens(WdfRound round)
    {
        double voteEnd = round.StartTime + VoteDurationSeconds;
        WdfScreen vote = new()
        {
            Type = "vote",
            StartTime = round.StartTime,
            EndTime = voteEnd,
            VoteInfo = new WdfVoteScreenInfo
            {
                VoteOptions = round.VoteOptions,
                VoteStartTime = round.StartTime,
                VoteEndTime = round.StartTime + VoteChoiceDurationSeconds,
                VoteResultFetchTime = round.StartTime + VoteResultDelaySeconds
            }
        };
        if (round.MapName is not string mapName)
            return [vote];

        double waitingEnd = round.GameplayEndTime + WaitingDurationSeconds;
        return
        [
            vote,
            new WdfScreen
            {
                Type = "vote-lobby",
                StartTime = voteEnd,
                EndTime = round.GameplayStartTime,
                MapName = mapName
            },
            new WdfScreen
            {
                Type = "in-game",
                StartTime = round.GameplayStartTime,
                EndTime = round.GameplayEndTime,
                MapName = mapName
            },
            new WdfScreen
            {
                Type = "waiting-screen",
                StartTime = round.GameplayEndTime,
                EndTime = waitingEnd,
                MapName = mapName,
                WaitingScreenInfo = new WdfWaitingScreenInfo
                {
                    GetRecapTime = waitingEnd - RecapFetchLeadSeconds
                }
            },
            new WdfScreen
            {
                Type = "vote-recap",
                StartTime = waitingEnd,
                EndTime = round.EndTime,
                MapName = mapName
            }
        ];
    }

    private double GetServerTime()
        => timeProvider.GetUtcNow().ToUnixTimeMilliseconds() / 1000d;
}
