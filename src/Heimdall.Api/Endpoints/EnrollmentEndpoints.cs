using Heimdall.Api.Common;
using Heimdall.Api.Security;
using Heimdall.Application.Hosts;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enroll")
            .WithTags("Enrollment")
            .RequireRateLimiting("ingest")
            .AddEndpointFilter<EnrollmentKeyEndpointFilter>();

        group.MapPost("/", async (EnrollRequest request, EnrollHostHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return result.ToHttpResult(response => TypedResults.Ok(response));
        });

        return app;
    }
}
