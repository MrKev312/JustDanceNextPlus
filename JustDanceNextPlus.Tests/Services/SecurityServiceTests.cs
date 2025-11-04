using JustDanceNextPlus.Services;

using Microsoft.Extensions.Logging;

using Moq;

namespace JustDanceNextPlus.Tests.Services;

public class SecurityServiceTests
{
    [Fact]
    public void Constructor_InitializesKey_IsNotNullAndCorrectLength()
    {
		// Arrange
		ILogger<SecurityService> mockLogger = Mock.Of<ILogger<SecurityService>>();

		// Act
		SecurityService securityService = new(mockLogger);
		IReadOnlyList<byte> key = securityService.Secret256bit;

        // Assert
        Assert.NotNull(key);
        Assert.Equal(32, key.Count); // 256 bits = 32 bytes
    }

    [Fact]
    public void Constructor_CalledTwice_GeneratesDifferentKeys()
    {
		// Arrange
		ILogger<SecurityService> mockLogger = Mock.Of<ILogger<SecurityService>>();

		// Act
		SecurityService service1 = new(mockLogger);
		SecurityService service2 = new(mockLogger);

        IReadOnlyList<byte> key1 = service1.Secret256bit;
        IReadOnlyList<byte> key2 = service2.Secret256bit;

        // Assert
        // This test verifies that the key generation is random and not a static or hardcoded value.
        Assert.NotEqual(key1, key2);
    }
}