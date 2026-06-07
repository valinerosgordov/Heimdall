using Heimdall.Application.Metrics;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.Metrics;

namespace Heimdall.Application.Abstractions;

public interface IMetricRepository
{
    /// <summary>Bulk-inserts samples into the time-series store.</summary>
    Task InsertSamplesAsync(IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken);

    /// <summary>Returns the points of one metric for one host within [from, to], ordered by time.</summary>
    Task<IReadOnlyList<MetricPoint>> QueryAsync(
        HostId hostId,
        string metric,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken);

    /// <summary>Downsampled query: averages each metric into <paramref name="bucketSeconds"/> buckets (time_bucket).</summary>
    Task<IReadOnlyList<MetricPoint>> QueryBucketedAsync(
        HostId hostId,
        string metric,
        DateTimeOffset from,
        DateTimeOffset to,
        int bucketSeconds,
        CancellationToken cancellationToken);

    /// <summary>Latest value per (host, metric) for the requested metrics — drives the overview grid.</summary>
    Task<IReadOnlyList<LatestMetric>> GetLatestPerHostAsync(
        IReadOnlyList<string> metrics,
        CancellationToken cancellationToken);
}
