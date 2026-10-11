namespace JustDanceServer.Configuration;

public sealed class WdfSettings
{
    public int HappyHourCycleMinutes { get; init; } = 180;
    public int HappyHourDurationMinutes { get; init; } = 30;
    public DateTimeOffset HappyHourEpochUtc { get; init; } = DateTimeOffset.UnixEpoch;
}
