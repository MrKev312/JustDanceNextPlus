using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod.just_dance.com.account.v1;

[ApiController]
[Route("account/v1/accounts")]
public sealed class Accounts(ISessionManager sessionManager) : ControllerBase
{
    [HttpGet("{token}")]
    public IActionResult GetAccount([FromRoute] string token)
    {
        if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Session? session))
            return Unauthorized();

        return Ok(new AccountResponse
        {
            Pid = session.PlayerId,
            Ticket = Request.Headers.Authorization.FirstOrDefault() ?? string.Empty,
            Token = token,
            OtherPids = []
        });
    }
}

public sealed record AccountResponse
{
    public required Guid Pid { get; init; }
    public required string Ticket { get; init; }
    public required string Token { get; init; }
    public Guid[] OtherPids { get; init; } = [];
}
