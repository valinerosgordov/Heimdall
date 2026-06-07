using Heimdall.Api.Common;
using Heimdall.Application.HealthChecks;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class HealthCheckEndpoints
{
    public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/healthchecks").WithTags("HealthChecks").RequireAuthorization();

        group.MapPost("/", async (
            CreateHealthCheckRequest request,
            CreateHealthCheckTargetHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return result.ToHttpResult(dto => TypedResults.Created($"/api/healthchecks/{dto.Id}", dto));
        });

        group.MapGet("/", async (ListHealthChecksHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(cancellationToken)));

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteHealthCheckTargetHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.ToHttpResult(() => TypedResults.NoContent());
        });

        group.MapGet("/{id:guid}/history", async (
            Guid id,
            int? minutes,
            GetHealthCheckHistoryHandler handler,
            CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(id, minutes ?? 60, cancellationToken)));

        return app;
    }
}
