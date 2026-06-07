namespace Heimdall.Contracts;

/// <summary>Dashboard login (single operator account).</summary>
public sealed record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

/// <summary>A signed access token the dashboard sends as a Bearer header (and via ?access_token= for SSE).</summary>
public sealed record LoginResponse
{
    public required string AccessToken { get; init; }
    public required long ExpiresAtUnixMs { get; init; }
}

/// <summary>First-run: create the single operator account (only works when none exists yet).</summary>
public sealed record SetupRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

/// <summary>Whether an operator account exists yet — drives the login vs first-run-setup screen.</summary>
public sealed record AuthStatusResponse
{
    public required bool Configured { get; init; }
}
