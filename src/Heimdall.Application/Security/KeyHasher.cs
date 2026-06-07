using System.Security.Cryptography;
using System.Text;

namespace Heimdall.Application.Security;

/// <summary>Hashes agent keys (SHA-256, hex) and verifies them in constant time.</summary>
public static class KeyHasher
{
    public static string Hash(string key)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public static bool Verify(string providedKey, string storedHashHex)
    {
        var providedHash = Hash(providedKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedHash),
            Encoding.UTF8.GetBytes(storedHashHex));
    }
}
