using JustDanceNextPlus.Services;

using System;
using System.Globalization;

namespace JustDanceNextPlus.Tests.Services;

public class TimingServiceTests
{
    private readonly TimingService _timingService;

    public TimingServiceTests()
    {
        _timingService = new TimingService();
    }

    [Fact]
    public void TimeString_WithKnownDateTime_FormatsCorrectly()
    {
		// Arrange
		// Using a specific DateTime with Utc kind to be precise
		DateTime specificTime = new(2025, 10, 26, 14, 30, 55, DateTimeKind.Utc);
		string expectedString = "2025-10-26T14:30:55Z";

		// Act
		string result = _timingService.TimeString(specificTime);

        // Assert
        Assert.Equal(expectedString, result);
    }

    [Fact]
    public void ServerTime_ReturnsCurrentUtcTime_InCorrectFormat()
    {
		// Arrange
		DateTime timeBefore = DateTime.UtcNow;

		// Act
		string resultString = _timingService.ServerTime();

		// Arrange
		DateTime timeAfter = DateTime.UtcNow;

		// Assert
		string format = "yyyy-MM-ddTHH:mm:ssZ";
		bool success = DateTime.TryParseExact(resultString, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedTime);

        Assert.True(success, $"The returned string '{resultString}' was not in the expected format '{format}'.");

		// Truncate the bounds to the nearest second to match the precision of the service's output.
		DateTime lowerBound = timeBefore.AddSeconds(-1);
		DateTime upperBound = timeAfter.AddSeconds(1);

        // Verify the parsed time is within the window of the method call, accounting for truncation.
        Assert.InRange(parsedTime, lowerBound, upperBound);
    }
}