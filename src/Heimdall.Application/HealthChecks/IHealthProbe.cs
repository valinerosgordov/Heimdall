using Heimdall.Domain.HealthChecks;

namespace Heimdall.Application.HealthChecks;

/// <summary>Probes a target of a specific kind. One implementation per <see cref="ProbeKind"/>.</summary>
public interface IHealthProbe
{
    ProbeKind Kind { get; }

    Task<HealthProbeResult> ProbeAsync(HealthCheckTarget target, CancellationToken cancellationToken);
}
