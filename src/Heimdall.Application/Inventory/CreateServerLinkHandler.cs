using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

public sealed class CreateServerLinkHandler(IServerRepository repository)
{
    public async Task<Result<ServerLinkDto>> HandleAsync(CreateServerLinkRequest request, CancellationToken cancellationToken)
    {
        var link = ServerLink.Create(request.FromServerId, request.ToServerId, request.Kind);
        if (link.IsFailure)
            return link.Error;

        await repository.AddLinkAsync(link.Value, cancellationToken);
        return new ServerLinkDto
        {
            Id = link.Value.Id,
            FromServerId = link.Value.From.Value,
            ToServerId = link.Value.To.Value,
            Kind = link.Value.Kind,
        };
    }
}
