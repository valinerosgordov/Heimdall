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
            // Trust only a well-formed, full-length secret — never silently use a truncated/tampered file.
            if (IsStrong(existing))
                return existing;
        }

        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        File.WriteAllText(path, secret);
        Restrict(path);
        return secret;
    }

    private static bool IsStrong(string secret)
    {
        try
        {
            return Convert.FromBase64String(secret).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void Restrict(string path)
    {
        if (OperatingSystem.IsWindows())
            return; // LocalApplicationData is already per-user on Windows.
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // best effort
        }
    }
}
