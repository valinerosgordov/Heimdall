using Heimdall.Application.Alerting;

namespace Heimdall.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings").RequireAuthorization();

        group.MapGet("/channels", (GetChannelStatusHandler handler) => TypedResults.Ok(handler.Handle()));

        group.MapPost("/channels/test", async (SendTestNotificationHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(cancellationToken)));

        return app;
    }
}
