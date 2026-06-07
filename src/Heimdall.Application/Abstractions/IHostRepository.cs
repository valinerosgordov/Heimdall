using Heimdall.Application.Hosts;
using Heimdall.Domain.Hosts;

namespace Heimdall.Application.Abstractions;

public interface IHostRepository
{
    /// <summary>Loads a host (including its agent key hash) by unique name, or null if unknown.</summary>
    Task<MonitoredHost?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Inserts the host or, if the name exists, updates its agent key hash and last-seen time.</summary>
    Task EnrollAsync(MonitoredHost host, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Refreshes a host's last-seen time (called on each ingest).</summary>
    Task TouchAsync(HostId hostId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>All hosts for the overview, ordered by name.</summary>
    Task<IReadOnlyList<HostSummary>> ListAsync(CancellationToken cancellationToken);
}
