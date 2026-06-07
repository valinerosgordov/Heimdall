using Heimdall.Application.Abstractions;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

public sealed class DeleteServerLinkHandler(IServerRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteLinkAsync(id, cancellationToken);
        return deleted
            ? Result.Success()
            : Result.Failure(Error.NotFound("ServerLink.NotFound", "Server link not found."));
    }
}
