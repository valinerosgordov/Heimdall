using Dapper;
using Heimdall.Application.Abstractions;
using Heimdall.Application.HealthChecks;
using Heimdall.Domain.HealthChecks;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

internal sealed class HealthCheckRepository(NpgsqlDataSource dataSource) : IHealthCheckRepository
{
    public async Task AddAsync(HealthCheckTarget target, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO healthcheck_targets (id, name, kind, target, interval_seconds, enabled, created_at, last_checked_at)
            VALUES (@id, @name, @kind, @target, @intervalSeconds, @enabled, @createdAt, @lastCheckedAt);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                id = target.Id.Value,
                name = target.Name,
                kind = target.Kind.ToString(),
                target = target.Target,
                intervalSeconds = target.IntervalSeconds,
                enabled = target.Enabled,
                createdAt = target.CreatedAt.UtcDateTime,
                lastCheckedAt = target.LastCheckedAt?.UtcDateTime,
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<HealthCheckTarget>> GetEnabledAsync(CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT id, name, kind, target, interval_seconds, enabled, created_at, last_checked_at
            FROM healthcheck_targets
            WHERE enabled = TRUE;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<TargetRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));

        return
        [
            .. rows.Select(r => HealthCheckTarget.FromStorage(
                new HealthCheckTargetId(r.Id),
                r.Name,
                Enum.Parse<ProbeKind>(r.Kind, ignoreCase: true),
                r.Target,
                r.IntervalSeconds,
                r.Enabled,
                AsUtc(r.CreatedAt),
                AsUtc(r.LastCheckedAt))),
        ];
    }

    public async Task<IReadOnlyList<HealthCheckStatus>> ListWithStatusAsync(CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT t.id, t.name, t.kind, t.target, t.enabled, t.last_checked_at,
                   r.is_up, r.latency_ms,
                   (SELECT round(100.0 * sum(CASE WHEN u.is_up THEN 1 ELSE 0 END) / count(*), 1)::double precision
                    FROM healthcheck_results u
                    WHERE u.target_id = t.id AND u.time > now() - INTERVAL '24 hours') AS uptime24h
            FROM healthcheck_targets t
            LEFT JOIN LATERAL (
                SELECT is_up, latency_ms
                FROM healthcheck_results r
                WHERE r.target_id = t.id
                ORDER BY time DESC
                LIMIT 1
            ) r ON TRUE
            ORDER BY t.name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StatusRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));

        return
        [
            .. rows.Select(r => new HealthCheckStatus(
                r.Id,
                r.Name,
                Enum.Parse<ProbeKind>(r.Kind, ignoreCase: true),
                r.Target,
                r.Enabled,
                r.IsUp,
                r.LatencyMs,
                AsUtc(r.LastCheckedAt),
                r.Uptime24h)),
        ];
    }

    public async Task<bool> DeleteAsync(HealthCheckTargetId id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM healthcheck_results WHERE target_id = @id;",
            new { id = id.Value },
            cancellationToken: cancellationToken));

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM healthcheck_targets WHERE id = @id;",
            new { id = id.Value },
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task RecordResultAsync(HealthCheckTargetId id, bool isUp, double latencyMs, DateTimeOffset at, CancellationToken cancellationToken)
    {
        const string sql =
            "INSERT INTO healthcheck_results (time, target_id, is_up, latency_ms) VALUES (@time, @targetId, @isUp, @latencyMs);";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { time = at.UtcDateTime, targetId = id.Value, isUp, latencyMs },
            cancellationToken: cancellationToken));
    }

    public async Task MarkCheckedAsync(HealthCheckTargetId id, DateTimeOffset at, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE healthcheck_targets SET last_checked_at = @at WHERE id = @id;",
            new { id = id.Value, at = at.UtcDateTime },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<HealthCheckResultRow>> QueryResultsAsync(
        HealthCheckTargetId id,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT (extract(epoch FROM time) * 1000)::bigint AS timestamp_unix_ms,
                   is_up,
                   latency_ms
            FROM healthcheck_results
            WHERE target_id = @id AND time >= @from AND time <= @to
            ORDER BY time;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<HealthCheckResultRow>(new CommandDefinition(
            sql,
            new { id = id.Value, from = from.UtcDateTime, to = to.UtcDateTime },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? AsUtc(DateTime? value)
        => value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private sealed record TargetRow(
        Guid Id, string Name, string Kind, string Target, int IntervalSeconds, bool Enabled, DateTime CreatedAt, DateTime? LastCheckedAt);

    private sealed record StatusRow(
        Guid Id, string Name, string Kind, string Target, bool Enabled, DateTime? LastCheckedAt, bool? IsUp, double? LatencyMs, double? Uptime24h);
}
