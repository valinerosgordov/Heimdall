using Heimdall.Api.Common;
using Heimdall.Api.Security;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, JwtTokenService tokens) =>
                tokens.Login(request).ToHttpResult(response => TypedResults.Ok(response)))
            .AllowAnonymous()
            .RequireRateLimiting("ingest")
            .WithTags("Auth");

        return app;
    }
}
