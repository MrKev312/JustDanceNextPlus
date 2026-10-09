using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public sealed record WdfCurrentMap(
	string MapName,
	double GameplayStartTime,
	double GameplayEndTime);

public interface IWdfService
{
	WdfRoomId GetAssignedRoom(int gameYear);
	bool TryResolveRoom(string requestedRoom, int gameYear, out WdfRoomId room);
	WdfCurrentMap? GetCurrentMap(WdfRoomId room);
	WdfHappyHoursInfo GetHappyHours();
	WdfScreenList GetScreens(WdfRoomId room, double? startTime, Guid? playerId = null);
	WdfVoteResult GetVoteResult(WdfRoomId room);
	bool SubmitVote(WdfRoomId room, Guid playerId, string mapName);
	ValueTask<WdfInitScoreResult> InitializeScoreAsync(
		WdfRoomId room,
		Guid playerId,
		string platform);
	ValueTask<WdfUpdateScoreResult> UpdateScoreAsync(
		WdfRoomId room,
		Guid playerId,
		string platform,
		double score);
	WdfRecapInfo GetRecap(WdfRoomId room, Guid playerId);
	WdfSessionSummary GetSessionSummary(Guid playerId);
	void BeginSession(WdfRoomId room, Guid playerId);
	void Touch(WdfRoomId room, Guid playerId);
	void Leave(WdfRoomId room, Guid playerId);
	int GetActivePlayerCount(WdfRoomId room);
}

public interface IWdfCommandService
{
	void Enqueue(WdfRoomId room, string mapName);
	void EnqueueSet(WdfRoomId room, IEnumerable<string> mapNames);
	void ReplaceQueue(WdfRoomId room, IEnumerable<string> mapNames);
	void ReplaceQueueSets(WdfRoomId room, IEnumerable<IEnumerable<string>> mapSets);
	IReadOnlyList<string> GetQueue(WdfRoomId room);
	IReadOnlyList<IReadOnlyList<string>> GetQueueSets(WdfRoomId room);
}
