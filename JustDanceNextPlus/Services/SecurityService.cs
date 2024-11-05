using System.Security.Cryptography;

namespace JustDanceNextPlus.Services;

public class SecurityService
{
	public byte[] Secret256bit { get; private set; }

	public SecurityService(ILogger<SecurityService> logger)
	{
		// Generate a random 256-bit key
		Secret256bit = GenerateRandom256BitKey();

		logger.LogInformation("Generated 256-bit key: {secret}", Secret256bit);
	}

	// Method to generate a random 256-bit key
	private static byte[] GenerateRandom256BitKey()
	{
		byte[] key = new byte[32]; // 32 bytes = 256 bits
		using (RandomNumberGenerator random = RandomNumberGenerator.Create())
		{
			random.GetBytes(key); // Fill the array with cryptographically strong random bytes
		}

		return key;
	}
}
