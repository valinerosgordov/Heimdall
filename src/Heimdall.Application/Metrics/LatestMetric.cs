namespace Heimdall.Application.Metrics;

/// <summary>Read model: the most recent value of one metric for one host.</summary>
public readonly record struct LatestMetric(Guid HostId, string Metric, double Value);
