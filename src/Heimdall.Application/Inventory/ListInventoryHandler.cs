using Heimdall.Application.Abstractions;
using Heimdall.Contracts;

namespace Heimdall.Application.Inventory;

public sealed class ListInventoryHandler(IServerRepository repository, TimeProvider clock)
{
    public async Task<InventoryResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var servers = await repository.ListWithStatusAsync(cancellationToken);
        var links = await repository.ListLinksAsync(cancellationToken);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        return new InventoryResponse
        {
            Servers = [.. servers.Select(s => ServerMappings.ToDto(s, today))],
            Links =
            [
                .. links.Select(l => new ServerLinkDto
                {
                    Id = l.Id,
                    FromServerId = l.From.Value,
                    ToServerId = l.To.Value,
                    Kind = l.Kind,
                }),
            ],
        };
    }
}
