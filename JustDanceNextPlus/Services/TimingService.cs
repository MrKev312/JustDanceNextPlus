namespace JustDanceNextPlus.Services;

public interface ITimingService
{
	string ServerTime();
	string TimeString(DateTime time);
}

public class TimingService : ITimingService
{
	public string TimeString(DateTime time)
	{
		return time.ToString("yyyy-MM-ddTHH:mm:ssZ");
	}

	public string ServerTime()
	{
		return TimeString(DateTime.UtcNow);
	}
}
