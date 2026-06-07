using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Metrics;

/// <summary>
/// Returns the requested metric series for a host over the last N minutes (clamped 1–1440),
/// automatically downsampling (time_bucket avg) when the window is too wide for raw points.
/// </summary>
public sealed class QueryHostMetricsHandler(IHostRepository hosts, IMetricRepository metrics, TimeProvider clock)
{
    // Agent cadence is ~5s; below this, bucketing buys nothing so we serve raw points.
    private const int RawCadenceSeconds = 5;

    public async Task<Result<HostMetricsResponse>> HandleAsync(
        string hostName,
        IReadOnlyList<string> metricNames,
        int minutes,
        int maxPoints,
        CancellationToken cancellationToken)
    {
        var host = await hosts.GetByNameAsync(hostName, cancellationToken);
        if (host is null)
            return Error.NotFound("Host.NotFound", $"Host '{hostName}' was not found.");

        var to = clock.GetUtcNow();
        var from = to.AddMinutes(-Math.Clamp(minutes, 1, 1440));

        var targetPoints = Math.Clamp(maxPoints, 60, 2000);
        var windowSeconds = (to - from).TotalSeconds;
        var bucketSeconds = (int)Math.Ceiling(windowSeconds / targetPoints);
        var downsample = bucketSeconds > RawCadenceSeconds;

        var series = new List<MetricSeriesDto>(metricNames.Count);
        foreach (var metric in metricNames)
        {
            var points = downsample
                ? await metrics.QueryBucketedAsync(host.Id, metric, from, to, bucketSeconds, cancellationToken)
                : await metrics.QueryAsync(host.Id, metric, from, to, cancellationToken);

            series.Add(new MetricSeriesDto
            {
                Metric = metric,
                Points = [.. points.Select(p => new MetricPointDto { TimestampUnixMs = p.TimestampUnixMs, Value = p.Value })],
            });
        }

        return new HostMetricsResponse { HostName = host.Name, Series = series };
    }
}
