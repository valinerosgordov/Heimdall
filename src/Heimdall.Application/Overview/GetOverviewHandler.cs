using Heimdall.Application.Abstractions;
using Heimdall.Contracts;

namespace Heimdall.Application.Overview;

/// <summary>Builds the overview snapshot: every host with its latest values + online state, plus health-check statuses.</summary>
public sealed class GetOverviewHandler(
    IHostRepository hosts,
    IMetricRepository metrics,
    IHealthCheckRepository healthChecks,
    TimeProvider clock)
{
    private static readonly string[] OverviewMetrics = [MetricNames.CpuUsage, MetricNames.MemoryUsage, MetricNames.DiskUsage];
    private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(30);

    public async Task<OverviewSnapshot> HandleAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var hostList = await hosts.ListAsync(cancellationToken);
        var latest = await metrics.GetLatestPerHostAsync(OverviewMetrics, cancellationToken);
        var byHost = latest
            .GroupBy(l => l.HostId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Metric, x => x.Value));

        var hostDtos = hostList.Select(host =>
        {
            byHost.TryGetValue(host.Id, out var values);
            double? Value(string metric) => values is not null && values.TryGetValue(metric, out var v) ? v : null;

            return new OverviewHostDto
            {
                Id = host.Id,
                Name = host.Name,
                LastSeenAtUnixMs = host.LastSeenAt.ToUnixTimeMilliseconds(),
                Online = now - host.LastSeenAt <= OnlineWindow,
                Cpu = Value(MetricNames.CpuUsage),
                Memory = Value(MetricNames.MemoryUsage),
                Disk = Value(MetricNames.DiskUsage),
            };
        }).ToList();

        var statuses = await healthChecks.ListWithStatusAsync(cancellationToken);
        var healthDtos = statuses.Select(s => new HealthCheckStatusDto
        {
            Id = s.Id,
            Name = s.Name,
            Kind = s.Kind.ToString(),
            Target = s.Target,
            Enabled = s.Enabled,
            IsUp = s.IsUp,
            LatencyMs = s.LatencyMs,
            LastCheckedAtUnixMs = s.LastCheckedAt?.ToUnixTimeMilliseconds(),
        }).ToList();

        return new OverviewSnapshot
        {
            GeneratedAtUnixMs = now.ToUnixTimeMilliseconds(),
            Hosts = hostDtos,
            HealthChecks = healthDtos,
        };
    }
}
