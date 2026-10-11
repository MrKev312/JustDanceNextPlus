using Microsoft.AspNetCore.Mvc;

using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Controllers.prod.just_dance.com.carousel.v2.pages;

[ApiController]
[Route("carousel/v2/pages/upsell-videos")]
public class UpsellVideos : ControllerBase
{
    [HttpPost]
    public IActionResult GetUpsellVideos()
    {
        return Ok(new JduCarouselContentResponse());
    }
}
