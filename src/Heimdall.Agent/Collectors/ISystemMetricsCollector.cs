namespace Heimdall.Agent.Collectors;

/// <summary>One metric reading produced by a collector.</summary>
public readonly record struct MetricReading(string Metric, double Value);

/// <summary>Collects the full set of system metrics for one tick.</summary>
public interface ISystemMetricsCollector
{
    ValueTask<IReadOnlyList<MetricReading>> CollectAsync(CancellationToken cancellationToken);
}

/// <summary>Platform-specific source for CPU% and memory% (the parts that need OS APIs).</summary>
public interface ICpuMemorySource
{
    ValueTask<(double CpuPercent, double MemoryPercent)> ReadAsync(CancellationToken cancellationToken);
}
