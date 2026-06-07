using System.Text.Json;
using Heimdall.Application.Overview;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class OverviewEndpoints
{
    private static readonly TimeSpan StreamInterval = TimeSpan.FromSeconds(2);

    public static IEndpointRouteBuilder MapOverviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/overview", async (GetOverviewHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(cancellationToken)))
            .WithTags("Overview")
            .RequireAuthorization();

        // Server-Sent Events: emit a snapshot immediately, then every StreamInterval until the client disconnects.
        app.MapGet("/api/stream/overview", StreamOverviewAsync).WithTags("Overview").RequireAuthorization();

        return app;
    }

    private static async Task StreamOverviewAsync(
        HttpContext context,
        GetOverviewHandler handler,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        using var timer = new PeriodicTimer(StreamInterval);
        try
        {
            do
            {
                var snapshot = await handler.HandleAsync(cancellationToken);
                var json = JsonSerializer.Serialize(snapshot, HeimdallJsonContext.Default.OverviewSnapshot);
                await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — normal end of stream.
        }
    }
}
