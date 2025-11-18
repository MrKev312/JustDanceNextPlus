using System.Collections.Concurrent;

namespace JustDanceNextPlus.Services;

public class DashboardService(ILogger<DashboardService> logger)
{
    public event Action? OnStateChanged;

    public ConcurrentQueue<RequestLog> RecentRequests { get; } = [];
    public ConcurrentQueue<ScoreLog> RecentScores { get; } = [];

    public void LogRequest(string codeName, string mapName, string username)
    {
        //RecentRequests.Insert(0, new RequestLog(mapName, username, DateTime.Now));
        RecentRequests.Enqueue(new RequestLog(codeName, mapName, username, DateTime.Now));
        while (RecentRequests.Count > 50)
            RecentRequests.TryDequeue(out _);

        logger.LogInformation("DashboardService: Request logged. Notifying state change.");
        NotifyStateChanged();
    }

    public void LogScore(string mapName, string username, int score, string type)
    {
        RecentScores.Enqueue(new ScoreLog(mapName, username, score, type, DateTime.Now));
        while (RecentScores.Count > 50)
            RecentScores.TryDequeue(out _);

        logger.LogInformation("DashboardService: Score logged. Notifying state change.");
        NotifyStateChanged();
    }

    public void NotifyStateChanged()
    {
        logger.LogInformation("DashboardService: Invoking OnStateChanged.");
        OnStateChanged?.Invoke();
    }
}

public record RequestLog(string CodeName, string MapName, string Username, DateTime Timestamp);
public record ScoreLog(string ItemName, string Username, int Score, string Type, DateTime Timestamp);
