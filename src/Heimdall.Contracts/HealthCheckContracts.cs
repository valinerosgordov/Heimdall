namespace Heimdall.Contracts;

/// <summary>Request to register a new health-check target.</summary>
public sealed record CreateHealthCheckRequest
{
    public required string Name { get; init; }

    /// <summary>"Http" or "Tcp".</summary>
    public required string Kind { get; init; }

    /// <summary>HTTP(S) URL for Http checks, or "host:port" for Tcp checks.</summary>
    public required string Target { get; init; }

    /// <summary>Probe cadence in seconds (clamped server-side).</summary>
    public int IntervalSeconds { get; init; } = 30;
}

/// <summary>A health-check target plus its most recent result (if any).</summary>
public sealed record HealthCheckStatusDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required string Target { get; init; }
    public required bool Enabled { get; init; }
    public bool? IsUp { get; init; }
    public double? LatencyMs { get; init; }
    public long? LastCheckedAtUnixMs { get; init; }

    /// <summary>Availability over the last 24h as a percentage (0–100), or null if no probes yet.</summary>
    public double? Uptime24h { get; init; }
}

/// <summary>One probe result over time.</summary>
public sealed record HealthCheckResultPointDto
{
    public required long TimestampUnixMs { get; init; }
    public required bool IsUp { get; init; }
    public required double LatencyMs { get; init; }
}

/// <summary>Result history for a single target.</summary>
public sealed record HealthCheckHistoryResponse
{
    public required Guid TargetId { get; init; }
    public required IReadOnlyList<HealthCheckResultPointDto> Points { get; init; }
}
