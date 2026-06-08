using Heimdall.Application.Inventory;
using Heimdall.Domain.Inventory;

namespace Heimdall.Application.Abstractions;

public interface IServerRepository
{
    Task AddAsync(Server server, CancellationToken cancellationToken);

    /// <summary>Race-safe create-or-update of an auto-discovered server, keyed on linked host name.</summary>
    Task UpsertDiscoveredAsync(Server server, CancellationToken cancellationToken);

    Task<Server?> GetAsync(ServerId id, CancellationToken cancellationToken);

    /// <summary>Finds the server bound to an agent host (linked_host_name, else name) for auto-discovery upsert.</summary>
    Task<Server?> FindByHostNameAsync(string hostName, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Server server, CancellationToken cancellationToken);

    /// <summary>All servers with the latest liveness of their linked health-check, for the inventory board.</summary>
    Task<IReadOnlyList<ServerRecord>> ListWithStatusAsync(CancellationToken cancellationToken);

    /// <summary>Deletes a server and any topology links touching it. Returns false if it did not exist.</summary>
    Task<bool> DeleteAsync(ServerId id, CancellationToken cancellationToken);

    Task AddLinkAsync(ServerLink link, CancellationToken cancellationToken);

    Task<bool> DeleteLinkAsync(Guid linkId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ServerLink>> ListLinksAsync(CancellationToken cancellationToken);
}
