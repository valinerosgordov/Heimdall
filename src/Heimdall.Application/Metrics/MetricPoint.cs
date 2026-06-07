namespace Heimdall.Application.Metrics;

/// <summary>Read model for one time-series point. Timestamp is Unix epoch milliseconds (UTC).</summary>
public readonly record struct MetricPoint(long TimestampUnixMs, double Value);
