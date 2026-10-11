namespace JustDanceServer.Services;

public interface IClassicJduConstantService
{
    IReadOnlyDictionary<string, object> CreateConstants();
}

public sealed class ClassicJduConstantService : IClassicJduConstantService
{
    public IReadOnlyDictionary<string, object> CreateConstants()
    {
        return new Dictionary<string, object>
        {
            ["HttpRequest"] = new Dictionary<string, object>
            {
                ["timeout"] = new Dictionary<string, object>
                {
                    ["activity"] = 30_000
                }
            },
            ["Subscription_Service"] = new Dictionary<string, object>
            {
                ["ServerRefresh"] = new Dictionary<string, object>
                {
                    ["retry_interval"] = 60,
                    ["refresh_interval"] = 600,
                    ["retry_interval_s2s"] = 600
                }
            },
            ["WDF"] = new Dictionary<string, object>
            {
                ["Recap"] = new Dictionary<string, object>
                {
                    ["recap_retry_interval"] = 2
                },
                ["UpdateScore"] = new Dictionary<string, object>
                {
                    ["update_score_interval"] = 5,
                    ["update_failure_allowance"] = 10
                },
                ["Screens"] = new Dictionary<string, object>
                {
                    ["screen_prefetch_duration"] = 5,
                    ["screens_retry_interval"] = 2
                }
            }
        };
    }
}
