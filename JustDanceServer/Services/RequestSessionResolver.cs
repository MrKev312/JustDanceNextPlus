using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace JustDanceServer.Services;

public static class RequestSessionResolver
{
	private static readonly string[] SessionHeaders = ["x-jmcs-sessionid", "ubi-sessionid"];

	public static bool TryResolve(
		HttpRequest request,
		ISessionManager sessionManager,
		[NotNullWhen(true)] out Session? session)
	{
		foreach (string header in SessionHeaders)
		{
			if (Guid.TryParse(request.Headers[header].FirstOrDefault(), out Guid sessionId)
				&& sessionManager.TryGetSessionById(sessionId, out Session? found))
			{
				sessionManager.TouchSession(sessionId);
				session = found;
				return true;
			}
		}

		string authorization = request.Headers.Authorization.FirstOrDefault() ?? string.Empty;
		const string ubiPrefix = "Ubi_v1 ";
		if (authorization.StartsWith(ubiPrefix, StringComparison.OrdinalIgnoreCase))
		{
			string ticket = authorization[ubiPrefix.Length..].Trim();
			if (ticket.StartsWith("t=", StringComparison.OrdinalIgnoreCase))
				ticket = ticket[2..].Trim();
			JwtSecurityTokenHandler handler = new();
			if (handler.CanReadToken(ticket))
			{
				JwtSecurityToken token = handler.ReadJwtToken(ticket);
				string? sessionValue = token.Claims.FirstOrDefault(claim => claim.Type == "sid")?.Value;
				if (Guid.TryParse(sessionValue, out Guid sessionId)
					&& sessionManager.TryGetSessionById(sessionId, out Session? found))
				{
					sessionManager.TouchSession(sessionId);
					session = found;
					return true;
				}
			}
		}

		session = null;
		return false;
	}

	public static string GetPlatform(HttpRequest request)
	{
		string requestedPlatform = request.Headers["Ubi-RequestedPlatformType"].FirstOrDefault() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(requestedPlatform))
			return requestedPlatform.Trim().ToLowerInvariant();

		string sku = request.Headers["X-SkuId"].FirstOrDefault() ?? string.Empty;
		if (sku.Contains("wiiu", StringComparison.OrdinalIgnoreCase))
			return "wiiu";
		if (sku.Contains("-nx-", StringComparison.OrdinalIgnoreCase))
			return "switch";
		if (sku.Contains("-pc-", StringComparison.OrdinalIgnoreCase))
			return "uplay";
		if (sku.Contains("-x1-", StringComparison.OrdinalIgnoreCase))
			return "x1";
		if (sku.Contains("-ps4-", StringComparison.OrdinalIgnoreCase))
			return "ps4";

		return "unknown";
	}
}
