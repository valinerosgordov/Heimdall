using Heimdall.Domain.Alerting;

namespace Heimdall.Application.Abstractions;

public interface IAlertRepository
{
    Task AddRuleAsync(AlertRule rule, CancellationToken cancellationToken);

    Task<IReadOnlyList<AlertRule>> ListRulesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken cancellationToken);

    Task<bool> DeleteRuleAsync(AlertRuleId id, CancellationToken cancellationToken);

    /// <summary>The currently firing alert for a (rule, host) pair, or null.</summary>
    Task<Alert?> GetActiveAlertAsync(AlertRuleId ruleId, string hostName, CancellationToken cancellationToken);

    Task InsertAlertAsync(Alert alert, CancellationToken cancellationToken);

    Task ResolveAlertAsync(AlertId id, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Firing alerts first, then the most recent resolved ones, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<Alert>> ListAlertsAsync(int limit, CancellationToken cancellationToken);
}
