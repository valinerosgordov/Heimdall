using System.Security.Cryptography;

namespace Heimdall.Application.Security;

/// <summary>
/// Salted PBKDF2 (SHA-256) hashing for the human-chosen operator password. Distinct from
/// <see cref="KeyHasher"/> (fast unsalted SHA-256), which is appropriate only for high-entropy agent keys.
/// Encoded as <c>pbkdf2$sha256$&lt;iterations&gt;$&lt;saltB64&gt;$&lt;hashB64&gt;</c> so parameters can evolve.
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"pbkdf2$sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 5 || parts[0] != "pbkdf2" || parts[1] != "sha256")
            return false;
        if (!int.TryParse(parts[2], out var iterations) || iterations < 1)
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[3]);
            expected = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
