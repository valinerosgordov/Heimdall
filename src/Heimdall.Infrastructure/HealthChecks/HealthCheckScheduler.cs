using System.Collections.Frozen;
using Heimdall.Application.Abstractions;
using Heimdall.Application.HealthChecks;
using Heimdall.Domain.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Heimdall.Infrastructure.HealthChecks;

/// <summary>Probes due health-check targets on a fixed tick and records results.</summary>
internal sealed class HealthCheckScheduler(
    IHealthCheckRepository repository,
    IEnumerable<IHealthProbe> probes,
    TimeProvider clock,
    ILogger<HealthCheckScheduler> logger) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(5);
    private readonly FrozenDictionary<ProbeKind, IHealthProbe> _probes = probes.ToFrozenDictionary(p => p.Kind);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Health-check scheduler started ({ProbeCount} probe kinds).", _probes.Count);

        using var timer = new PeriodicTimer(Tick);
        do
        {
            try
            {
                await RunDueProbesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health-check scheduler tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunDueProbesAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var targets = await repository.GetEnabledAsync(cancellationToken);

        var due = targets.Where(t => t.IsDue(now)).ToArray();
        if (due.Length == 0)
            return;

        await Task.WhenAll(due.Select(t => ProbeAndRecordAsync(t, cancellationToken)));
    }

    private async Task ProbeAndRecordAsync(HealthCheckTarget target, CancellationToken cancellationToken)
    {
        if (!_probes.TryGetValue(target.Kind, out var probe))
            return;

        var result = await probe.ProbeAsync(target, cancellationToken);
        var at = clock.GetUtcNow();

        await repository.RecordResultAsync(target.Id, result.IsUp, result.LatencyMs, at, cancellationToken);
        await repository.MarkCheckedAsync(target.Id, at, cancellationToken);
    }
}
