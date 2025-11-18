using System.Collections.Immutable;
using System.Security.Cryptography;

namespace JustDanceNextPlus.Services;

public interface ISecurityService
{
    IReadOnlyList<byte> Secret256bit { get; }
    bool ValidateCredentials(string username, string password);
}

public class SecurityService : ISecurityService
{
	public IReadOnlyList<byte> Secret256bit { get; private set; }

	public SecurityService(ILogger<SecurityService> logger)
	{
		// Generate a random 256-bit key
		Secret256bit = GenerateRandom256BitKey();

		logger.LogInformation("Generated 256-bit key: {secret}", Secret256bit);
	}

	// Method to generate a random 256-bit key
	private static ImmutableArray<byte> GenerateRandom256BitKey()
	{
		byte[] key = new byte[32]; // 32 bytes = 256 bits
		RandomNumberGenerator.Fill(key);

        return [.. key];
	}

    public bool ValidateCredentials(string username, string password)
    {
        // Hardcoded credentials for now
        return username == "admin" && password == "admin";
    }
}
