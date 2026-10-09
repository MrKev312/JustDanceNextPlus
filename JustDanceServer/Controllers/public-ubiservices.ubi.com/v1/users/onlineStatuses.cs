using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.users;

[ApiController]
[Route("v1/users/onlineStatuses")]
public sealed class OnlineStatuses(ISessionManager sessionManager) : ControllerBase
{
	[HttpGet(Name = "GetOnlineStatuses")]
	public IActionResult GetOnlineStatuses()
	{
		Guid[] userIds = [.. Request.Query["userIds"].ToString()
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty)
			.Where(id => id != Guid.Empty)
			.Distinct()];
		if (userIds.Length == 0)
			return BadRequest(new { message = "Missing or invalid userIds" });

		DateTime now = DateTime.UtcNow;
		return Ok(new
		{
			onlineStatuses = userIds.Select(userId =>
			{
				Session[] sessions = [.. sessionManager.GetSessionsByPlayerId(userId)
					.Where(session => session.ExpiresAt > now)];
				return new
				{
					connections = sessions.Select(session => new
					{
						applicationId = session.UbiAppId,
						connectionProfileId = userId,
						createdAt = session.CreatedAt,
						lastModifiedAt = session.LastRefreshedAt,
						stagingSpaceId = string.Empty
					}),
					manuallySet = false,
					onlineStatus = sessions.Length > 0 ? "online" : "offline",
					userId
				};
			})
		});
	}
}
