using Heimdall.Domain.HealthChecks;

namespace Heimdall.Application.HealthChecks;

/// <summary>Read model: a target plus its latest result for the status board.</summary>
public readonly record struct HealthCheckStatus(
    Guid Id,
    string Name,
    ProbeKind Kind,
    string Target,
    bool Enabled,
    bool? IsUp,
    double? LatencyMs,
    DateTimeOffset? LastCheckedAt,
    double? Uptime24h);

/// <summary>Read model: one historical probe result.</summary>
public readonly record struct HealthCheckResultRow(long TimestampUnixMs, bool IsUp, double LatencyMs);
