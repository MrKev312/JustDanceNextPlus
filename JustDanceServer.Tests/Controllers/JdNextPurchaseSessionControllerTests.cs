using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Moq;

using System.Collections.Immutable;
using System.Text.Json;

using PurchaseResponse = JustDanceServer.Controllers.prod_next.just_dance.com.session.v2.PurchaseResponse;
using PurchaseSessionController = JustDanceServer.Controllers.prod_next.just_dance.com.session.v2.Session;
using ServiceSession = JustDanceServer.Services.Session;

namespace JustDanceServer.Tests.Controllers;

public sealed class JdNextPurchaseSessionControllerTests
{
	[Fact]
	public async Task EmptySessionIdRecoversTheUbiSessionByVerifiedPlatformIdentity()
	{
		const string subject = "nintendo-account-subject";
		const string nsaIdToken = "signed-nintendo-token";
		Guid sessionId = Guid.NewGuid();
		Guid playerId = Guid.NewGuid();
		Guid bootId = Guid.NewGuid();
		string identityKey = SessionIdentityKey.ForApplication(
			$"nintendo-account:{subject}",
			JdNextConstants.SwitchApplicationId);
		ServiceSession session = new()
		{
			SessionId = sessionId,
			PlayerId = playerId,
			UbiAppId = JdNextConstants.SwitchApplicationId
		};

		Mock<ISessionManager> sessionManager = new();
		sessionManager
			.Setup(manager => manager.GetSessionByIdentityKey(identityKey))
			.Returns(session);
		Mock<ITimingService> timingService = new();
		timingService.Setup(service => service.ServerTime()).Returns("2026-08-13T12:00:00Z");
		Mock<IBundleService> bundleService = new();
		bundleService.Setup(service => service.GetAllClaims()).Returns(ImmutableArray<string>.Empty);
		Mock<IPlatformTicketVerifierRegistry> platformTicketVerifiers = new();
		platformTicketVerifiers
			.Setup(verifiers => verifiers.VerifyAsync(
				"switch",
				nsaIdToken,
				It.IsAny<PlatformTicketContext>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(new VerifiedPlatformTicket(
				"nintendo-account",
				subject,
				subject));

		PurchaseSessionController controller = new(
			timingService.Object,
			sessionManager.Object,
			bundleService.Object,
			platformTicketVerifiers.Object)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			}
		};
		controller.Request.Headers["Ubi-SessionId"] = Guid.Empty.ToString();
		controller.Request.Headers["Ubi-AppId"] = JdNextConstants.SwitchApplicationId.ToString();

		using JsonDocument body = JsonDocument.Parse(JsonSerializer.Serialize(new
		{
			bootId,
			userOwnership = new
			{
				dlcCheckToken = new { nsaIdToken }
			}
		}));

		IActionResult result = await controller.PostSessions2(body.RootElement);

		OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
		PurchaseResponse response = Assert.IsType<PurchaseResponse>(ok.Value);
		Assert.Equal(sessionId.ToString(), response.SessionId);
		Assert.Equal(playerId.ToString(), response.ProfileId);
		Assert.Equal(bootId.ToString(), response.BootId);
		sessionManager.Verify(manager => manager.GetSessionByIdentityKey(identityKey), Times.Once);
		sessionManager.Verify(manager => manager.GetSessionByAppId(It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	public async Task RejectsAnUnverifiedOwnershipTicket()
	{
		Mock<ITimingService> timingService = new();
		Mock<ISessionManager> sessionManager = new();
		Mock<IBundleService> bundleService = new();
		Mock<IPlatformTicketVerifierRegistry> platformTicketVerifiers = new();
		platformTicketVerifiers
			.Setup(verifiers => verifiers.VerifyAsync(
				"switch",
				It.IsAny<string>(),
				It.IsAny<PlatformTicketContext>(),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync((VerifiedPlatformTicket?)null);

		PurchaseSessionController controller = new(
			timingService.Object,
			sessionManager.Object,
			bundleService.Object,
			platformTicketVerifiers.Object)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			}
		};
		controller.Request.Headers["Ubi-SessionId"] = Guid.Empty.ToString();
		using JsonDocument body = JsonDocument.Parse("""
			{
			  "userOwnership": {
			    "dlcCheckToken": { "nsaIdToken": "forged-token" }
			  }
			}
			""");

		IActionResult result = await controller.PostSessions2(body.RootElement);

		Assert.IsType<BadRequestObjectResult>(result);
		sessionManager.Verify(
			manager => manager.GetSessionByIdentityKey(It.IsAny<string>()),
			Times.Never);
	}
}
