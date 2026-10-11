using JustDanceServer.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public interface IBasicCredentialVerifier
{
    bool Verify(string login, string password);
}

public sealed class BasicCredentialVerifier(IOptions<PlatformAuthenticationSettings> settings) : IBasicCredentialVerifier
{
    private readonly PasswordHasher<string> hasher = new();

    public bool Verify(string login, string password)
    {
        string? hash = settings.Value.BasicPasswordHashes.FirstOrDefault(
            pair => pair.Key.Equals(login, StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(hash))
            return false;
        try
        {
            return hasher.VerifyHashedPassword(login, hash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
