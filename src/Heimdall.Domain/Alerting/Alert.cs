using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Alerting;

public enum AlertStatus
{
    Firing,
    Resolved,
}

/// <summary>An instance of a rule breaching for a host — fires, then resolves when the breach clears.</summary>
public sealed class Alert : AggregateRoot<AlertId>
{
    private Alert() { }

    private Alert(
        AlertId id,
        AlertRuleId ruleId,
        string ruleName,
        string hostName,
        string metric,
        AlertSeverity severity,
        AlertStatus status,
        double value,
        DateTimeOffset startedAt,
        DateTimeOffset? resolvedAt) : base(id)
    {
        RuleId = ruleId;
        RuleName = ruleName;
        HostName = hostName;
        Metric = metric;
        Severity = severity;
        Status = status;
        Value = value;
        StartedAt = startedAt;
        ResolvedAt = resolvedAt;
    }

    public AlertRuleId RuleId { get; private set; }
    public string RuleName { get; private set; } = string.Empty;
    public string HostName { get; private set; } = string.Empty;
    public string Metric { get; private set; } = string.Empty;
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; }
    public double Value { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public static Alert Fire(AlertRule rule, string hostName, double value, DateTimeOffset now)
        => new(AlertId.New(), rule.Id, rule.Name, hostName, rule.Metric, rule.Severity, AlertStatus.Firing, value, now, resolvedAt: null);

    public void Resolve(DateTimeOffset now)
    {
        Status = AlertStatus.Resolved;
        ResolvedAt = now;
    }

    public static Alert FromStorage(
        AlertId id,
        AlertRuleId ruleId,
        string ruleName,
        string hostName,
        string metric,
        AlertSeverity severity,
        AlertStatus status,
        double value,
        DateTimeOffset startedAt,
        DateTimeOffset? resolvedAt)
        => new(id, ruleId, ruleName, hostName, metric, severity, status, value, startedAt, resolvedAt);
}
