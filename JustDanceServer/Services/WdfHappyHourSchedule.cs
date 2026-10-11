using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public sealed class WdfHappyHourSchedule
{
    private readonly TimeProvider timeProvider;
    private readonly double cycleSeconds;
    private readonly double durationSeconds;
    private readonly double epochSeconds;

    public WdfHappyHourSchedule(IOptions<WdfSettings> options, TimeProvider timeProvider)
    {
        WdfSettings settings = options.Value;
        if (settings.HappyHourCycleMinutes <= 0)
            throw new InvalidOperationException("WDF Happy Hour cycle must be positive.");
        if (settings.HappyHourDurationMinutes <= 0
            || settings.HappyHourDurationMinutes >= settings.HappyHourCycleMinutes)
        {
            throw new InvalidOperationException(
                "WDF Happy Hour duration must be positive and shorter than its cycle.");
        }

        this.timeProvider = timeProvider;
        cycleSeconds = TimeSpan.FromMinutes(settings.HappyHourCycleMinutes).TotalSeconds;
        durationSeconds = TimeSpan.FromMinutes(settings.HappyHourDurationMinutes).TotalSeconds;
        epochSeconds = settings.HappyHourEpochUtc.ToUnixTimeMilliseconds() / 1000d;
    }

    public bool IsRunning => GetInfo().Running;

    public WdfHappyHoursInfo GetInfo()
    {
        double now = timeProvider.GetUtcNow().ToUnixTimeMilliseconds() / 1000d;
        double cycleIndex = Math.Floor((now - epochSeconds) / cycleSeconds);
        double currentStart = epochSeconds + (cycleIndex * cycleSeconds);
        double currentEnd = currentStart + durationSeconds;
        bool running = now >= currentStart && now < currentEnd;

        double advertisedStart = running ? currentStart : currentStart + cycleSeconds;
        return new WdfHappyHoursInfo
        {
            Start = advertisedStart,
            End = advertisedStart + durationSeconds,
            Running = running
        };
    }
}
