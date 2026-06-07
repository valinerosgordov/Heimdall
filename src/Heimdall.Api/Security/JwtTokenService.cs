using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Heimdall.Application.Security;
using Heimdall.Contracts;
using Heimdall.Domain.SharedKernel;
using Microsoft.IdentityModel.Tokens;

namespace Heimdall.Api.Security;

/// <summary>Validates the single operator account and issues HS256 access tokens.</summary>
internal sealed class JwtTokenService(IConfiguration configuration, TimeProvider clock)
{
    public const string Issuer = "heimdall";
    public const string Audience = "heimdall";

    public Result<LoginResponse> Login(LoginRequest request)
    {
        var username = configuration["Heimdall:Auth:Username"];
        var passwordHash = configuration["Heimdall:Auth:PasswordSha256"];
        var secret = configuration["Heimdall:Jwt:Secret"];

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(secret))
            return Error.Failure("Auth.NotConfigured", "Authentication is not configured on the server.");

        // Evaluate both unconditionally (no short-circuit) so a wrong username doesn't leak timing.
        var usernameOk = string.Equals(request.Username, username, StringComparison.Ordinal);
        var passwordOk = KeyHasher.Verify(request.Password, passwordHash);

        if (!usernameOk || !passwordOk)
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password.");

        var lifetime = TimeSpan.FromHours(configuration.GetValue("Heimdall:Jwt:LifetimeHours", 8));
        var now = clock.GetUtcNow();
        var expires = now.Add(lifetime);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(ClaimTypes.Name, username)],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponse { AccessToken = accessToken, ExpiresAtUnixMs = expires.ToUnixTimeMilliseconds() };
    }
}
