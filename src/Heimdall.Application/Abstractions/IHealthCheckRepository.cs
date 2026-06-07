using Heimdall.Application.HealthChecks;
using Heimdall.Domain.HealthChecks;

namespace Heimdall.Application.Abstractions;

public interface IHealthCheckRepository
{
    Task AddAsync(HealthCheckTarget target, CancellationToken cancellationToken);

    /// <summary>Enabled targets, for the scheduler to probe.</summary>
    Task<IReadOnlyList<HealthCheckTarget>> GetEnabledAsync(CancellationToken cancellationToken);

    /// <summary>All targets with their latest result, for the status board.</summary>
    Task<IReadOnlyList<HealthCheckStatus>> ListWithStatusAsync(CancellationToken cancellationToken);

    /// <summary>Deletes a target and its results. Returns false if it did not exist.</summary>
    Task<bool> DeleteAsync(HealthCheckTargetId id, CancellationToken cancellationToken);

    Task RecordResultAsync(HealthCheckTargetId id, bool isUp, double latencyMs, DateTimeOffset at, CancellationToken cancellationToken);

    Task MarkCheckedAsync(HealthCheckTargetId id, DateTimeOffset at, CancellationToken cancellationToken);

    Task<IReadOnlyList<HealthCheckResultRow>> QueryResultsAsync(
        HealthCheckTargetId id,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);
}
