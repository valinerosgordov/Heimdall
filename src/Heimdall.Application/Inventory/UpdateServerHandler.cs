using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

public sealed class UpdateServerHandler(IServerRepository repository, TimeProvider clock)
{
    public async Task<Result<ServerDto>> HandleAsync(Guid id, CreateServerRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetAsync(new ServerId(id), cancellationToken);
        if (existing is null)
            return Error.NotFound("Server.NotFound", "Server not found.");

        var date = ServerMappings.ParseDate(request.PaidUntil);
        if (date.IsFailure)
            return date.Error;

        var now = clock.GetUtcNow();
        var updated = existing.Update(ServerMappings.ToDraft(request, date.Value), now);
        if (updated.IsFailure)
            return updated.Error;

        await repository.UpdateAsync(existing, cancellationToken);
        return ServerMappings.ToDto(ServerMappings.ToRecord(existing), DateOnly.FromDateTime(now.UtcDateTime));
    }
}
