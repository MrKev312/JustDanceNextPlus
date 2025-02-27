﻿using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/claims-offers-db")]
public class ClaimsOffersDB(MapService mapService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetClaimsOffersDb()
	{
		return Ok(mapService.SongDB.SongDBTypeSet.SongOffers.Claims);
	}
}
