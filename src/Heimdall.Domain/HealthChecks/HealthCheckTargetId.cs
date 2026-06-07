namespace Heimdall.Domain.HealthChecks;

/// <summary>Strongly-typed identity for a health-check target (UUIDv7, time-ordered).</summary>
public readonly record struct HealthCheckTargetId(Guid Value)
{
    public static HealthCheckTargetId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
