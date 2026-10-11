using System.Collections.Immutable;
using System.Security.Cryptography;

namespace JustDanceServer.Services;

public interface ISecurityService
{
    IReadOnlyList<byte> Secret256bit { get; }
}

public class SecurityService : ISecurityService
{
    public IReadOnlyList<byte> Secret256bit { get; } = GenerateRandom256BitKey();

    // Method to generate a random 256-bit key
    private static ImmutableArray<byte> GenerateRandom256BitKey()
    {
        byte[] key = new byte[32]; // 32 bytes = 256 bits
        RandomNumberGenerator.Fill(key);

        return [.. key];
    }
}
