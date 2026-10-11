using System.Text;
using System.Text.Json;
using JustDanceServer.Configuration;
using JustDanceServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Tests.Services;

public sealed class BasicCredentialVerifierTests
{
    [Theory]
    [InlineData("", "uplay")]
    [InlineData("%%%", "uplay")]
    [InlineData("dXNlcg==", "uplay")]
    [InlineData("OnBhc3N3b3Jk", "uplay")]
    [InlineData("dXNlcjpwYXNzd29yZA==", "switch")]
    public async Task InvalidOrCrossPlatformBasicCredentialsAreRejected(string credentials, string platform)
    {
        using JsonDocument body = JsonDocument.Parse("{}");
        BasicCredentialVerifier verifier = new(Options.Create(new PlatformAuthenticationSettings
        {
            BasicPasswordHashes = new() { ["user"] = new PasswordHasher<string>().HashPassword("user", "password") }
        }));
        SessionLoginIdentityParser parser = new(new PlatformTicketVerifierRegistry([]), verifier);
        Assert.Null(await parser.ParseAsync("Basic " + credentials, body.RootElement, platform, "Player"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-password-hash")]
    [InlineData("AA==")]
    public void MissingOrInvalidConfigurationFailsClosed(string? hash)
    {
        PlatformAuthenticationSettings settings = new();
        if (hash is not null)
            settings.BasicPasswordHashes["user"] = hash;
        Assert.False(new BasicCredentialVerifier(Options.Create(settings)).Verify("user", "password"));
    }

    [Fact]
    public async Task BasicPasswordsMayContainColonsAndUsernamesAreCaseInsensitive()
    {
        using JsonDocument body = JsonDocument.Parse("{}");
        BasicCredentialVerifier verifier = new(Options.Create(new PlatformAuthenticationSettings
        {
            BasicPasswordHashes = new() { ["User"] = new PasswordHasher<string>().HashPassword("user", "a:b:c") }
        }));
        SessionLoginIdentityParser parser = new(new PlatformTicketVerifierRegistry([]), verifier);
        string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("USER:a:b:c"));
        Assert.Equal("ubisoft-login:user", (await parser.ParseAsync(authorization, body.RootElement, "uplay", "Player"))!.SessionKey);
    }
}
