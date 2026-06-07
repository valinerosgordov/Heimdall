using Dapper;
using Heimdall.Application.Abstractions;
using Heimdall.Domain.Alerting;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

internal sealed class AlertRepository(NpgsqlDataSource dataSource) : IAlertRepository
{
    public async Task AddRuleAsync(AlertRule rule, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO alert_rules (id, name, metric, operator, threshold, duration_seconds, severity, host_name, enabled, created_at)
            VALUES (@id, @name, @metric, @operator, @threshold, @durationSeconds, @severity, @hostName, @enabled, @createdAt);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            id = rule.Id.Value,
            name = rule.Name,
            metric = rule.Metric,
            @operator = ComparisonOperatorParser.ToWire(rule.Operator),
            threshold = rule.Threshold,
            durationSeconds = rule.DurationSeconds,
            severity = rule.Severity.ToString(),
            hostName = rule.HostName,
            enabled = rule.Enabled,
            createdAt = rule.CreatedAt.UtcDateTime,
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<AlertRule>> ListRulesAsync(CancellationToken cancellationToken)
        => await QueryRulesAsync("SELECT * FROM alert_rules ORDER BY created_at DESC;", cancellationToken);

    public async Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken cancellationToken)
        => await QueryRulesAsync("SELECT * FROM alert_rules WHERE enabled = TRUE;", cancellationToken);

    public async Task<bool> DeleteRuleAsync(AlertRuleId id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM alerts WHERE rule_id = @id;", new { id = id.Value }, cancellationToken: cancellationToken));
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM alert_rules WHERE id = @id;", new { id = id.Value }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<Alert?> GetActiveAlertAsync(AlertRuleId ruleId, string hostName, CancellationToken cancellationToken)
    {
        const string sql =
            "SELECT * FROM alerts WHERE rule_id = @ruleId AND host_name = @hostName AND status = 'Firing' LIMIT 1;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<AlertRow>(new CommandDefinition(
            sql, new { ruleId = ruleId.Value, hostName }, cancellationToken: cancellationToken));
        return row is null ? null : MapAlert(row);
    }

    public async Task InsertAlertAsync(Alert alert, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO alerts (id, rule_id, rule_name, host_name, metric, severity, status, value, started_at, resolved_at)
            VALUES (@id, @ruleId, @ruleName, @hostName, @metric, @severity, @status, @value, @startedAt, @resolvedAt);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            id = alert.Id.Value,
            ruleId = alert.RuleId.Value,
            ruleName = alert.RuleName,
            hostName = alert.HostName,
            metric = alert.Metric,
            severity = alert.Severity.ToString(),
            status = alert.Status.ToString(),
            value = alert.Value,
            startedAt = alert.StartedAt.UtcDateTime,
            resolvedAt = alert.ResolvedAt?.UtcDateTime,
        }, cancellationToken: cancellationToken));
    }

    public async Task ResolveAlertAsync(AlertId id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE alerts SET status = 'Resolved', resolved_at = @now WHERE id = @id;",
            new { id = id.Value, now = now.UtcDateTime },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Alert>> ListAlertsAsync(int limit, CancellationToken cancellationToken)
    {
        const string sql =
            "SELECT * FROM alerts ORDER BY (status = 'Firing') DESC, started_at DESC LIMIT @limit;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AlertRow>(new CommandDefinition(sql, new { limit }, cancellationToken: cancellationToken));
        return [.. rows.Select(MapAlert)];
    }

    private async Task<IReadOnlyList<AlertRule>> QueryRulesAsync(string sql, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AlertRuleRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return [.. rows.Select(MapRule)];
    }

    private static AlertRule MapRule(AlertRuleRow r) => AlertRule.FromStorage(
        new AlertRuleId(r.Id),
        r.Name,
        r.Metric,
        ComparisonOperatorParser.Parse(r.Operator).Value,
        r.Threshold,
        r.DurationSeconds,
        Enum.Parse<AlertSeverity>(r.Severity, ignoreCase: true),
        r.HostName,
        r.Enabled,
        AsUtc(r.CreatedAt));

    private static Alert MapAlert(AlertRow r) => Alert.FromStorage(
        new AlertId(r.Id),
        new AlertRuleId(r.RuleId),
        r.RuleName,
        r.HostName,
        r.Metric,
        Enum.Parse<AlertSeverity>(r.Severity, ignoreCase: true),
        Enum.Parse<AlertStatus>(r.Status, ignoreCase: true),
        r.Value,
        AsUtc(r.StartedAt),
        r.ResolvedAt is null ? null : AsUtc(r.ResolvedAt.Value));

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record AlertRuleRow(
        Guid Id, string Name, string Metric, string Operator, double Threshold, int DurationSeconds,
        string Severity, string? HostName, bool Enabled, DateTime CreatedAt);

    private sealed record AlertRow(
        Guid Id, Guid RuleId, string RuleName, string HostName, string Metric, string Severity, string Status,
        double Value, DateTime StartedAt, DateTime? ResolvedAt);
}
