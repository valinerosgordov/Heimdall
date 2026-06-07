namespace Heimdall.Contracts;

/// <summary>A batch of samples pushed by an agent for a single host.</summary>
public sealed record MetricBatchRequest
{
    public required string HostName { get; init; }
    public required IReadOnlyList<MetricSampleDto> Samples { get; init; }
}

/// <summary>One pushed sample. Timestamp is Unix epoch milliseconds (UTC).</summary>
public sealed record MetricSampleDto
{
    public required string Metric { get; init; }
    public required double Value { get; init; }
    public required long TimestampUnixMs { get; init; }
}

/// <summary>Result of an ingest call.</summary>
public sealed record IngestResponse
{
    public required int Accepted { get; init; }
    public required int Rejected { get; init; }
}
