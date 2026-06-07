using Heimdall.Api.Common;
using Heimdall.Application.Alerting;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var rules = app.MapGroup("/api/alerts/rules").WithTags("Alerts").RequireAuthorization();

        rules.MapPost("/", async (CreateAlertRuleRequest request, CreateAlertRuleHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return result.ToHttpResult(dto => TypedResults.Created($"/api/alerts/rules/{dto.Id}", dto));
        });

        rules.MapGet("/", async (ListAlertRulesHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(cancellationToken)));

        rules.MapDelete("/{id:guid}", async (Guid id, DeleteAlertRuleHandler handler, CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.ToHttpResult(() => TypedResults.NoContent());
        });

        app.MapGet("/api/alerts", async (int? limit, ListAlertsHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(limit ?? 100, cancellationToken)))
            .WithTags("Alerts")
            .RequireAuthorization();

        return app;
    }
}
