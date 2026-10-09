using JustDanceServer.Configuration;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json;

namespace JustDanceServer.Controllers.prod.just_dance.com.subscription.v1;

[ApiController]
[Route("subscription/v1/refresh")]
public class Refresh : ControllerBase
{
    [HttpPost]
    public IActionResult RefreshSubscription([FromBody] JsonElement request)
    {
        // The platform-specific Classic clients send different parameter maps.
        // Subscription state is server-owned, so none of those request fields are
        // required to construct the response.
        _ = request;

        return Ok(new SubscriptionRefreshResponse
        {
            Validity = true,
            TimeLeft = 99_999_999_999,
            ExpiryTimeStamp = "99999999999",
            PlatformId = JduConstants.SubscriptionPlatformId,
            TrialActivated = true,
            ConsoleHasTrial = false,
            TrialIsActive = true,
            TrialDuration = "0",
            AutoRefresh = true
        });
    }
}

public record SubscriptionRefreshResponse
{
    public bool Validity { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public long TimeLeft { get; init; }
    public string ExpiryTimeStamp { get; init; } = string.Empty;
    public Guid PlatformId { get; init; }
    public bool TrialActivated { get; init; }
    public bool ConsoleHasTrial { get; init; }
    public long TrialTimeLeft { get; init; }
    public string TrialDuration { get; init; } = "0";
    public bool TrialIsActive { get; init; }
    public bool NeedEshopLink { get; init; }
    public bool AutoRefresh { get; init; }
}
