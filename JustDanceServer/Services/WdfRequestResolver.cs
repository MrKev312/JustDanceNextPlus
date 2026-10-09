using JustDanceServer.Configuration;

namespace JustDanceServer.Services;

public static class WdfRequestResolver
{
	public static bool TryGetGameYear(HttpRequest request, out int gameYear)
	{
		gameYear = 0;
		return JduMediaClientProfiles.TryResolve(request.Headers["X-SkuId"].ToString(), out JduMediaClientProfile? profile)
			&& (gameYear = profile.GameYear) != 0;
	}

}
