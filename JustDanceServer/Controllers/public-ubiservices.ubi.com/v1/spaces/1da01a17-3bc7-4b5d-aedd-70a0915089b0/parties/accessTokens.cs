using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0.parties;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/parties/{partyId:guid}/accessTokens")]
public sealed class AccessTokens(IPartyService partyService, ISessionManager sessionManager) : ControllerBase
{
	[HttpPost]
	public IActionResult Post(
		[FromRoute] Guid spaceId,
		[FromRoute] Guid partyId,
		[FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AccessTokenRequest? request)
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
			return Unauthorized();
		PartyAccessToken? token = partyService.IssueAccessToken(
			partyId,
			session.PlayerId,
			request?.ProfileId);
		return token is null ? StatusCode(StatusCodes.Status403Forbidden) : Ok(new
		{
			accessToken = token.Value,
			createdAt = token.CreatedAt,
			expiresAt = token.ExpiresAt
		});
	}
}

public sealed record AccessTokenRequest(Guid? ProfileId);
