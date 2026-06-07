using Heimdall.Contracts;
using Heimdall.Domain.Alerting;

namespace Heimdall.Application.Alerting;

internal static class AlertMappings
{
    public static AlertRuleDto ToDto(this AlertRule rule) => new()
    {
        Id = rule.Id.Value,
        Name = rule.Name,
        Metric = rule.Metric,
        Operator = ComparisonOperatorParser.ToWire(rule.Operator),
        Threshold = rule.Threshold,
        DurationSeconds = rule.DurationSeconds,
        Severity = rule.Severity.ToString().ToLowerInvariant(),
        HostName = rule.HostName,
        Enabled = rule.Enabled,
    };

    public static AlertDto ToDto(this Alert alert) => new()
    {
        Id = alert.Id.Value,
        RuleId = alert.RuleId.Value,
        RuleName = alert.RuleName,
        HostName = alert.HostName,
        Metric = alert.Metric,
        Severity = alert.Severity.ToString().ToLowerInvariant(),
        Status = alert.Status.ToString().ToLowerInvariant(),
        Value = alert.Value,
        StartedAtUnixMs = alert.StartedAt.ToUnixTimeMilliseconds(),
        ResolvedAtUnixMs = alert.ResolvedAt?.ToUnixTimeMilliseconds(),
    };
}
