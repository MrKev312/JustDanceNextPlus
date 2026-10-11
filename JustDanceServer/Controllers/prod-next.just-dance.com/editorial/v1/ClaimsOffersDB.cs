using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/claims-offers-db")]
public class ClaimsOffersDB(IMapService mapService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetClaimsOffersDb()
	{
		return Ok(mapService.SongDBTypeSet.SongOffers.Claims);
	}
}
