using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

using System.Security.Cryptography;

namespace JustDanceServer.Controllers.prod.just_dance.com.sessions.v1;

[ApiController]
[Route("sessions/v1/session")]
public sealed class Session(
	ISessionManager sessionManager,
	IPairingService pairingService) : ControllerBase
{
	[HttpPost]
	public IActionResult CreateSession([FromBody] JmcsSessionRequest request)
	{
		if (!RequestSessionResolver.TryResolve(Request, sessionManager, out Services.Session? session))
			return Unauthorized();

		if (!TryGetPairingAddress(request, out string address, out int port))
			return BadRequest("A valid pairing URL or public IP is required.");

		JmcsPairingInfo pairing = request.PairingInfo ?? new JmcsPairingInfo();
		PairingEntry entry = pairingService.CreatePairing(
			address,
			port,
			string.Empty,
			pairing.TlsCertificate,
			pairing.Protocol,
			pairing.TitleId,
			pairing.DisplayName,
			codeLength: 6);

		return Ok(new SessionResponse
		{
			PairingCode = entry.PairingCode,
			SessionId = session.SessionId,
			DocId = Convert.ToHexString(SHA1.HashData(session.SessionId.ToByteArray()))
				.ToLowerInvariant()
		});
	}

	private static bool TryGetPairingAddress(
		JmcsSessionRequest request,
		out string address,
		out int port)
	{
		if (Uri.TryCreate(request.PairingInfo?.PairingUrl, UriKind.Absolute, out Uri? uri)
			&& !string.IsNullOrWhiteSpace(uri.Host))
		{
			address = uri.Host;
			port = uri.IsDefaultPort ? (uri.Scheme == Uri.UriSchemeHttps ? 443 : 80) : uri.Port;
			return true;
		}

		address = request.PublicIp.Trim();
		port = 443;
		return !string.IsNullOrWhiteSpace(address);
	}
}

public sealed record JmcsSessionRequest
{
	public string PublicIp { get; init; } = string.Empty;
	public JmcsPairingInfo? PairingInfo { get; init; }
	public object[] Populations { get; init; } = [];
}

public sealed record JmcsPairingInfo
{
	public string Protocol { get; init; } = "v2.phonescoring.jd.ubisoft.com";
	public string PairingUrl { get; init; } = string.Empty;
	public string TlsCertificate { get; init; } = string.Empty;
	public string TitleId { get; init; } = string.Empty;
	public string DisplayName { get; init; } = "JustDanceServer";
}

public sealed record SessionResponse
{
	public required string PairingCode { get; init; }
	public required Guid SessionId { get; init; }
	public required string DocId { get; init; }
}
