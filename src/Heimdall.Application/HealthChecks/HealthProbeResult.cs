namespace Heimdall.Application.HealthChecks;

/// <summary>Outcome of a single probe.</summary>
public readonly record struct HealthProbeResult(bool IsUp, double LatencyMs, string? Error);
