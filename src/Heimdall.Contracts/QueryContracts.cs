namespace Heimdall.Contracts;

/// <summary>Overview row for a monitored host.</summary>
public sealed record HostSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required long CreatedAtUnixMs { get; init; }
    public required long LastSeenAtUnixMs { get; init; }
}

/// <summary>One point in a time series.</summary>
public sealed record MetricPointDto
{
    public required long TimestampUnixMs { get; init; }
    public required double Value { get; init; }
}

/// <summary>A single metric's series over the requested window.</summary>
public sealed record MetricSeriesDto
{
    public required string Metric { get; init; }
    public required IReadOnlyList<MetricPointDto> Points { get; init; }
}

/// <summary>All requested series for one host.</summary>
public sealed record HostMetricsResponse
{
    public required string HostName { get; init; }
    public required IReadOnlyList<MetricSeriesDto> Series { get; init; }
}
