using System.Security.Cryptography;

namespace Heimdall.Api.Security;

/// <summary>
/// Resolves the JWT signing secret: an explicit <c>Heimdall:Jwt:Secret</c> from config if set, otherwise
/// a 64-byte random secret persisted under the user's local app data — so a fresh install ships NO default
/// signing key and still survives restarts.
/// </summary>
internal sealed class JwtSecretProvider
{
    public string Secret { get; }

    public JwtSecretProvider(IConfiguration configuration)
    {
        var configured = configuration["Heimdall:Jwt:Secret"];
        Secret = !string.IsNullOrWhiteSpace(configured) ? configured : LoadOrCreate();
    }

    private static string LoadOrCreate()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Heimdall");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "jwt.secret");

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (existing.Length >= 32)
                return existing;
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        File.WriteAllText(path, secret);
        return secret;
    }
}
