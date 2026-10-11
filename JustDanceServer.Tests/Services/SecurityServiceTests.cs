using JustDanceServer.Services;

namespace JustDanceServer.Tests.Services;

public class SecurityServiceTests
{
    [Fact]
    public void Constructor_InitializesKey_IsNotNullAndCorrectLength()
    {
        // Act
        SecurityService securityService = new();
        IReadOnlyList<byte> key = securityService.Secret256bit;

        // Assert
        Assert.NotNull(key);
        Assert.Equal(32, key.Count); // 256 bits = 32 bytes
    }

    [Fact]
    public void Constructor_CalledTwice_GeneratesDifferentKeys()
    {
        // Act
        SecurityService service1 = new();
        SecurityService service2 = new();

        IReadOnlyList<byte> key1 = service1.Secret256bit;
        IReadOnlyList<byte> key2 = service2.Secret256bit;

        // Assert
        // This test verifies that the key generation is random and not a static or hardcoded value.
        Assert.NotEqual(key1, key2);
    }
}
