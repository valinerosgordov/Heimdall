using Heimdall.Api.Common;
using Heimdall.Application.Hosts;
using Heimdall.Application.Metrics;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class HostEndpoints
{
    private static readonly string[] DefaultMetrics = [MetricNames.CpuUsage, MetricNames.MemoryUsage];

    public static IEndpointRouteBuilder MapHostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/hosts").WithTags("Hosts").RequireAuthorization();

        group.MapGet("/", async (ListHostsHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(cancellationToken)));

        group.MapGet("/{hostName}/metrics", async (
            string hostName,
            string? names,
            int? minutes,
            int? maxPoints,
            QueryHostMetricsHandler handler,
            CancellationToken cancellationToken) =>
        {
            string[] metricNames = string.IsNullOrWhiteSpace(names)
                ? DefaultMetrics
                : names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var result = await handler.HandleAsync(hostName, metricNames, minutes ?? 15, maxPoints ?? 500, cancellationToken);
            return result.ToHttpResult(response => TypedResults.Ok(response));
        });

        return app;
    }
}
