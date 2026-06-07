using Heimdall.Api.Common;
using Heimdall.Api.Security;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // Drives the login vs first-run-setup screen on the client.
        group.MapGet("/status", async (JwtTokenService tokens, CancellationToken cancellationToken) =>
                TypedResults.Ok(new AuthStatusResponse { Configured = await tokens.IsConfiguredAsync(cancellationToken) }))
            .AllowAnonymous();

        // First-run: create the operator account. 409 once one exists.
        group.MapPost("/setup", async (SetupRequest request, JwtTokenService tokens, CancellationToken cancellationToken) =>
                (await tokens.SetupAsync(request, cancellationToken)).ToHttpResult(response => TypedResults.Ok(response)))
            .AllowAnonymous()
            .RequireRateLimiting("ingest");

        group.MapPost("/login", async (LoginRequest request, JwtTokenService tokens, CancellationToken cancellationToken) =>
                (await tokens.LoginAsync(request, cancellationToken)).ToHttpResult(response => TypedResults.Ok(response)))
            .AllowAnonymous()
            .RequireRateLimiting("ingest");

        return app;
    }
}
