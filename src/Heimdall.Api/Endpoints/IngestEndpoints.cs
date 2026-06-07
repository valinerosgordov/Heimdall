using Heimdall.Api.Common;
using Heimdall.Application.Inventory;
using Heimdall.Application.Metrics;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class IngestEndpoints
{
    private const string AgentKeyHeader = "X-Heimdall-Key";

    public static IEndpointRouteBuilder MapIngestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingest").WithTags("Ingest").RequireRateLimiting("ingest");

        group.MapPost("/metrics", async (
            MetricBatchRequest request,
            HttpRequest httpRequest,
            IngestMetricsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var agentKey = httpRequest.Headers[AgentKeyHeader].ToString();
            var result = await handler.HandleAsync(request, agentKey, cancellationToken);
            return result.ToHttpResult(response => TypedResults.Ok(response));
        });

        group.MapPost("/inventory", async (
            InventoryReportRequest request,
            HttpRequest httpRequest,
            ReportInventoryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var agentKey = httpRequest.Headers[AgentKeyHeader].ToString();
            var result = await handler.HandleAsync(request, agentKey, cancellationToken);
            return result.ToHttpResult(() => TypedResults.Ok());
        });

        return app;
    }
}
