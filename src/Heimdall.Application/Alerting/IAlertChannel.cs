using Heimdall.Domain.Alerting;

namespace Heimdall.Application.Alerting;

/// <summary>A fired or resolved alert handed to notification channels.</summary>
public sealed record AlertNotification(
    string RuleName,
    string HostName,
    string Metric,
    AlertSeverity Severity,
    double Value,
    bool Resolved,
    DateTimeOffset At);

/// <summary>A notification sink (Telegram, email, …). Unconfigured channels report <see cref="IsConfigured"/> false and are skipped.</summary>
public interface IAlertChannel
{
    string Name { get; }
    bool IsConfigured { get; }
    Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken);
}
