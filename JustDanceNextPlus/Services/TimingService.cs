namespace JustDanceNextPlus.Services;

public class TimingService
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
