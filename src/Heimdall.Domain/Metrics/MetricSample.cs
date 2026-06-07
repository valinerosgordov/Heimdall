using Heimdall.Domain.Hosts;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Metrics;

/// <summary>A single time-series data point: one host, one metric, one value, at one instant.</summary>
public sealed record MetricSample
{
    private MetricSample(HostId hostId, MetricName metric, double value, DateTimeOffset timestamp)
    {
        HostId = hostId;
        Metric = metric;
        Value = value;
        Timestamp = timestamp;
    }

    public HostId HostId { get; }
    public MetricName Metric { get; }
    public double Value { get; }
    public DateTimeOffset Timestamp { get; }

    public static Result<MetricSample> Create(HostId hostId, MetricName metric, double value, DateTimeOffset timestamp)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return Error.Validation("Metric.ValueInvalid", "Metric value must be a finite number.");

        return new MetricSample(hostId, metric, value, timestamp);
    }
}
