using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

public sealed class CreateServerHandler(IServerRepository repository, TimeProvider clock)
{
    public async Task<Result<ServerDto>> HandleAsync(CreateServerRequest request, CancellationToken cancellationToken)
    {
        var date = ServerMappings.ParseDate(request.PaidUntil);
        if (date.IsFailure)
            return date.Error;

        var now = clock.GetUtcNow();
        var server = Server.Create(ServerMappings.ToDraft(request, date.Value), now);
        if (server.IsFailure)
            return server.Error;

        await repository.AddAsync(server.Value, cancellationToken);
        return ServerMappings.ToDto(ServerMappings.ToRecord(server.Value), DateOnly.FromDateTime(now.UtcDateTime));
    }
}
