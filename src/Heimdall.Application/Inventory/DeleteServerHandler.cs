using Heimdall.Application.Abstractions;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

public sealed class DeleteServerHandler(IServerRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(new ServerId(id), cancellationToken);
        return deleted
            ? Result.Success()
            : Result.Failure(Error.NotFound("Server.NotFound", "Server not found."));
    }
}
