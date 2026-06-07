namespace Heimdall.Contracts;

/// <summary>One host tile on the overview grid: latest values + online state.</summary>
public sealed record OverviewHostDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required long LastSeenAtUnixMs { get; init; }
    public required bool Online { get; init; }
    public double? Cpu { get; init; }
    public double? Memory { get; init; }
    public double? Disk { get; init; }
}

/// <summary>A full snapshot of the monitoring overview — hosts and health checks at one instant.</summary>
public sealed record OverviewSnapshot
{
    public required long GeneratedAtUnixMs { get; init; }
    public required IReadOnlyList<OverviewHostDto> Hosts { get; init; }
    public required IReadOnlyList<HealthCheckStatusDto> HealthChecks { get; init; }
}
