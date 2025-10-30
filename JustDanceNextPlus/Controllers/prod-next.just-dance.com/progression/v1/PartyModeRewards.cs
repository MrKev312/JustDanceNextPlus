using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1;

[ApiController]
[Route("progression/v1/party-mode-rewards")]
public class PartyModeRewards : ControllerBase
{
	static readonly string[] value =
			[
				"f8f2b7bf-1199-46cd-8c7a-2d01333b50d4",
                "afd5e168-9988-47d2-bf4b-b36e49050870",
                "3c23a17d-5209-45d1-9362-a3bb878c1817",
                "d2dbbf49-4ebd-454b-9610-ac4ac6237986",
                "40535652-c9d1-40b5-855d-1adc0936a8e1"
            ];

	[HttpGet]
    public IActionResult GetPartyModeRewards()
    {
        return Ok(new
        {
            rewardsIds = value
		});
    }
}
