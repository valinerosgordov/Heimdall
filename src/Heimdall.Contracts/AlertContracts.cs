namespace Heimdall.Contracts;

/// <summary>Create an alert rule. Operator: gt/lt/gte/lte. Severity: warning/critical.</summary>
public sealed record CreateAlertRuleRequest
{
    public required string Name { get; init; }
    public required string Metric { get; init; }
    public required string Operator { get; init; }
    public required double Threshold { get; init; }
    public int DurationSeconds { get; init; } = 60;
    public string Severity { get; init; } = "warning";

    /// <summary>Limit the rule to one host, or null to evaluate every host.</summary>
    public string? HostName { get; init; }
}

public sealed record AlertRuleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Metric { get; init; }
    public required string Operator { get; init; }
    public required double Threshold { get; init; }
    public required int DurationSeconds { get; init; }
    public required string Severity { get; init; }
    public string? HostName { get; init; }
    public required bool Enabled { get; init; }
}

/// <summary>A fired alert. Status: firing/resolved.</summary>
public sealed record AlertDto
{
    public required Guid Id { get; init; }
    public required Guid RuleId { get; init; }
    public required string RuleName { get; init; }
    public required string HostName { get; init; }
    public required string Metric { get; init; }
    public required string Severity { get; init; }
    public required string Status { get; init; }
    public required double Value { get; init; }
    public required long StartedAtUnixMs { get; init; }
    public long? ResolvedAtUnixMs { get; init; }
}
