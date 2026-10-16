using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.progression.v1.profile;

[ApiController]
[Route("progression/v1/profile/me")]
public sealed class SavedDancerCardsController(
    ISavedDancerCardService cards, ISessionManager sessions, IPlayerMetadataService metadata) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (!RequestSessionResolver.TryResolve(Request, sessions, out Session? session))
			return Unauthorized();

        SavedDancerCardCollection? collection = await cards.GetAsync(session.PlayerId);
        if (collection is null)
			return NotFound();

        // The primary fields are flattened in profile, not wrapped in another dancercard object.
        return Ok(new { profile = new ProfileDetails(collection.Primary, collection.Cards) });
	}

	[HttpPost("dancercard")]
    public async Task<IActionResult> Create([FromBody] SavedDancerCardInput input)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessions, out Session? session))
			return Unauthorized();

        if (string.IsNullOrWhiteSpace(input.Name))
			return BadRequest("A dancer card name is required.");

        SavedDancerCardDetails? card = await cards.CreateAsync(session.PlayerId, input);

        return card is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { dancercard = card });
	}

	[HttpPatch("dancercard/{cardId:guid}")]
    public async Task<IActionResult> Update(Guid cardId, [FromBody] SavedDancerCardInput input)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessions, out Session? session))
			return Unauthorized();

        if (input.Name is not null && string.IsNullOrWhiteSpace(input.Name))
			return BadRequest("Invalid dancer card name.");

        SavedDancerCardDetails? card = await cards.UpdateAsync(session.PlayerId, cardId, input);

        if (card is null)
			return NotFound();

        SavedDancerCardCollection? collection = await cards.GetAsync(session.PlayerId);

        if (collection?.Primary.DancercardId == cardId)
            await metadata.UpdateAsync(session.PlayerId, countryCode: card.Country);

        return Ok(new { dancercard = card });
	}

	[HttpDelete("dancercard/{cardId:guid}")]
    public async Task<IActionResult> Delete(Guid cardId)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessions, out Session? session))
			return Unauthorized();

        return await cards.DeleteAsync(session.PlayerId, cardId) switch
        {
            DeleteDancerCardResult.Deleted => NoContent(),
            DeleteDancerCardResult.PrimaryCard => Conflict("The primary dancer card cannot be deleted."),
            _ => NotFound()
        };
	}

	private sealed record ProfileDetails : SavedDancerCardDetails
    {
        public IReadOnlyList<SavedDancerCardDetails> Dancercards { get; }

        public ProfileDetails(SavedDancerCardDetails primary, IReadOnlyList<SavedDancerCardDetails> cards)
            : base(primary.Name, primary.Country, primary.AvatarId, primary.PortraitBorderId,
                primary.AliasId, primary.AliasGender, primary.BackgroundId, primary.ScoringFxId,
                primary.VictoryFxId, primary.BadgesIds, primary.StickersIds, primary.CreatedAt,
                primary.UpdatedAt, primary.DancercardId) => Dancercards = cards;
    }
}
