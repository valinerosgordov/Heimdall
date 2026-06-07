using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Security;
using Heimdall.Contracts;
using Heimdall.Domain.SharedKernel;
using Microsoft.IdentityModel.Tokens;

namespace Heimdall.Api.Security;

/// <summary>First-run setup, login validation, and HS256 access-token issuance for the single operator.</summary>
internal sealed class JwtTokenService(
    IOperatorStore operators,
    JwtSecretProvider secrets,
    IConfiguration configuration,
    TimeProvider clock)
{
    public const string Issuer = "heimdall";
    public const string Audience = "heimdall";
    private const int MinPasswordLength = 8;
    private const int MaxUsernameLength = 100;

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
        => operators.IsConfiguredAsync(cancellationToken);

    public async Task<Result<LoginResponse>> SetupAsync(SetupRequest request, CancellationToken cancellationToken)
    {
        if (await operators.IsConfiguredAsync(cancellationToken))
            return Error.Conflict("Auth.AlreadyConfigured", "An operator account already exists.");

        var username = request.Username?.Trim() ?? string.Empty;
        if (username.Length is 0 or > MaxUsernameLength)
            return Error.Validation("Auth.InvalidUsername", $"Username must be 1–{MaxUsernameLength} characters.");
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length < MinPasswordLength)
            return Error.Validation("Auth.WeakPassword", $"Password must be at least {MinPasswordLength} characters.");

        await operators.SetAsync(username, KeyHasher.Hash(request.Password), cancellationToken);
        return Issue(username);
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var credentials = await operators.GetAsync(cancellationToken);
        if (credentials is null)
            return Error.Failure("Auth.NotConfigured", "No operator account yet — complete first-run setup.");

        // Evaluate both unconditionally (no short-circuit) so a wrong username doesn't leak timing.
        var usernameOk = string.Equals(request.Username, credentials.Value.Username, StringComparison.Ordinal);
        var passwordOk = KeyHasher.Verify(request.Password, credentials.Value.PasswordSha256);

        if (!usernameOk || !passwordOk)
            return Error.Unauthorized("Auth.InvalidCredentials", "Invalid username or password.");

        return Issue(credentials.Value.Username);
    }

    private Result<LoginResponse> Issue(string username)
    {
        var lifetime = TimeSpan.FromHours(configuration.GetValue("Heimdall:Jwt:LifetimeHours", 8));
        var now = clock.GetUtcNow();
        var expires = now.Add(lifetime);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secrets.Secret));
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
