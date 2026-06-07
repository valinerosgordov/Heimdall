using System.Net;
using Heimdall.Domain.Metrics;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Alerting;

/// <summary>A threshold rule: fires when <c>Metric</c> stays breaching for <c>DurationSeconds</c>.</summary>
public sealed class AlertRule : AggregateRoot<AlertRuleId>
{
    private const int MaxNameLength = 200;
    private const int MinDurationSeconds = 10;
    private const int MaxDurationSeconds = 3600;

    private AlertRule() { }

    private AlertRule(
        AlertRuleId id,
        string name,
        string metric,
        ComparisonOperator @operator,
        double threshold,
        int durationSeconds,
        AlertSeverity severity,
        string? hostName,
        bool enabled,
        DateTimeOffset createdAt) : base(id)
    {
        Name = name;
        Metric = metric;
        Operator = @operator;
        Threshold = threshold;
        DurationSeconds = durationSeconds;
        Severity = severity;
        HostName = hostName;
        Enabled = enabled;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; } = string.Empty;
    public string Metric { get; private set; } = string.Empty;
    public ComparisonOperator Operator { get; private set; }
    public double Threshold { get; private set; }
    public int DurationSeconds { get; private set; }
    public AlertSeverity Severity { get; private set; }

    /// <summary>Null means the rule applies to every host.</summary>
    public string? HostName { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<AlertRule> Create(
        string? name,
        string? metric,
        string? operatorRaw,
        double threshold,
        int durationSeconds,
        string? severityRaw,
        string? hostName,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Alert.NameEmpty", "Name must not be empty.");

        var cleanName = WebUtility.HtmlEncode(name.Trim());
        if (cleanName.Length > MaxNameLength)
            return Error.Validation("Alert.NameTooLong", $"Name must be {MaxNameLength} characters or fewer.");

        var metricName = MetricName.Create(metric);
        if (metricName.IsFailure)
            return metricName.Error;

        var @operator = ComparisonOperatorParser.Parse(operatorRaw);
        if (@operator.IsFailure)
            return @operator.Error;

        var severity = AlertSeverityParser.Parse(severityRaw);
        if (severity.IsFailure)
            return severity.Error;

        if (double.IsNaN(threshold) || double.IsInfinity(threshold))
            return Error.Validation("Alert.ThresholdInvalid", "Threshold must be a finite number.");

        var duration = Math.Clamp(durationSeconds, MinDurationSeconds, MaxDurationSeconds);
        var host = string.IsNullOrWhiteSpace(hostName) ? null : WebUtility.HtmlEncode(hostName.Trim());

        return new AlertRule(
            AlertRuleId.New(), cleanName, metricName.Value.Value, @operator.Value, threshold, duration, severity.Value, host, enabled: true, now);
    }

    public bool IsBreached(double value) => Operator switch
    {
        ComparisonOperator.GreaterThan => value > Threshold,
        ComparisonOperator.LessThan => value < Threshold,
        ComparisonOperator.GreaterOrEqual => value >= Threshold,
        ComparisonOperator.LessOrEqual => value <= Threshold,
        _ => false,
    };

    public static AlertRule FromStorage(
        AlertRuleId id,
        string name,
        string metric,
        ComparisonOperator @operator,
        double threshold,
        int durationSeconds,
        AlertSeverity severity,
        string? hostName,
        bool enabled,
        DateTimeOffset createdAt)
        => new(id, name, metric, @operator, threshold, durationSeconds, severity, hostName, enabled, createdAt);
}
