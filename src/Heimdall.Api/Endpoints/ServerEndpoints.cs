using Heimdall.Api.Common;
using Heimdall.Application.Inventory;
using Heimdall.Contracts;

namespace Heimdall.Api.Endpoints;

public static class ServerEndpoints
{
    public static IEndpointRouteBuilder MapServerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/servers").WithTags("Servers").RequireAuthorization();

        group.MapGet("/", async (ListInventoryHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(await handler.HandleAsync(cancellationToken)));

        group.MapPost("/", async (
            CreateServerRequest request,
            CreateServerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return result.ToHttpResult(dto => TypedResults.Created($"/api/servers/{dto.Id}", dto));
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            CreateServerRequest request,
            UpdateServerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, request, cancellationToken);
            return result.ToHttpResult(dto => TypedResults.Ok(dto));
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteServerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.ToHttpResult(() => TypedResults.NoContent());
        });

        group.MapPost("/links", async (
            CreateServerLinkRequest request,
            CreateServerLinkHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return result.ToHttpResult(dto => TypedResults.Created($"/api/servers/links/{dto.Id}", dto));
        });

        group.MapDelete("/links/{id:guid}", async (
            Guid id,
            DeleteServerLinkHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            return result.ToHttpResult(() => TypedResults.NoContent());
        });

        return app;
    }
}
