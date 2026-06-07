using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

/// <summary>Ensures the TimescaleDB schema exists on startup. Idempotent; retries while the DB is still starting.</summary>
internal sealed class DatabaseInitializer(NpgsqlDataSource dataSource, ILogger<DatabaseInitializer> logger) : IHostedService
{
    private const int MaxAttempts = 15;

    private const string SchemaSql = """
        CREATE EXTENSION IF NOT EXISTS timescaledb;

        CREATE TABLE IF NOT EXISTS hosts (
            id            uuid        PRIMARY KEY,
            name          text        NOT NULL UNIQUE,
            created_at    timestamptz NOT NULL,
            last_seen_at  timestamptz NOT NULL
        );

        CREATE TABLE IF NOT EXISTS metric_samples (
            time     timestamptz      NOT NULL,
            host_id  uuid             NOT NULL,
            metric   text             NOT NULL,
            value    double precision NOT NULL
        );

        SELECT create_hypertable('metric_samples', 'time', if_not_exists => TRUE);

        CREATE INDEX IF NOT EXISTS ix_metric_samples_host_metric_time
            ON metric_samples (host_id, metric, time DESC);

        ALTER TABLE hosts ADD COLUMN IF NOT EXISTS agent_key_hash text;

        CREATE TABLE IF NOT EXISTS healthcheck_targets (
            id               uuid        PRIMARY KEY,
            name             text        NOT NULL,
            kind             text        NOT NULL,
            target           text        NOT NULL,
            interval_seconds integer     NOT NULL,
            enabled          boolean     NOT NULL,
            created_at       timestamptz NOT NULL,
            last_checked_at  timestamptz NULL
        );

        CREATE TABLE IF NOT EXISTS healthcheck_results (
            time       timestamptz      NOT NULL,
            target_id  uuid             NOT NULL,
            is_up      boolean          NOT NULL,
            latency_ms double precision NOT NULL
        );

        SELECT create_hypertable('healthcheck_results', 'time', if_not_exists => TRUE);

        CREATE INDEX IF NOT EXISTS ix_healthcheck_results_target_time
            ON healthcheck_results (target_id, time DESC);

        CREATE TABLE IF NOT EXISTS alert_rules (
            id               uuid             PRIMARY KEY,
            name             text             NOT NULL,
            metric           text             NOT NULL,
            operator         text             NOT NULL,
            threshold        double precision NOT NULL,
            duration_seconds integer          NOT NULL,
            severity         text             NOT NULL,
            host_name        text             NULL,
            enabled          boolean          NOT NULL,
            created_at       timestamptz      NOT NULL
        );

        CREATE TABLE IF NOT EXISTS alerts (
            id          uuid             PRIMARY KEY,
            rule_id     uuid             NOT NULL,
            rule_name   text             NOT NULL,
            host_name   text             NOT NULL,
            metric      text             NOT NULL,
            severity    text             NOT NULL,
            status      text             NOT NULL,
            value       double precision NOT NULL,
            started_at  timestamptz      NOT NULL,
            resolved_at timestamptz      NULL
        );

        CREATE INDEX IF NOT EXISTS ix_alerts_active ON alerts (rule_id, host_name) WHERE status = 'Firing';
        CREATE INDEX IF NOT EXISTS ix_alerts_started ON alerts (started_at DESC);

        CREATE TABLE IF NOT EXISTS servers (
            id                    uuid             PRIMARY KEY,
            name                  text             NOT NULL,
            provider              text             NULL,
            ip_address            text             NULL,
            hostname              text             NULL,
            role                  text             NULL,
            cpu_cores             integer          NULL,
            ram_gb                double precision NULL,
            disk_gb               double precision NULL,
            location              text             NULL,
            monthly_cost          numeric          NULL,
            currency              text             NULL,
            paid_until            date             NULL,
            user_count            integer          NULL,
            notes                 text             NULL,
            linked_healthcheck_id uuid             NULL,
            linked_host_name      text             NULL,
            created_at            timestamptz      NOT NULL,
            updated_at            timestamptz      NOT NULL
        );

        CREATE TABLE IF NOT EXISTS server_links (
            id      uuid PRIMARY KEY,
            from_id uuid NOT NULL,
            to_id   uuid NOT NULL,
            kind    text NOT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_server_links_from ON server_links (from_id);
        CREATE INDEX IF NOT EXISTS ix_servers_paid_until ON servers (paid_until);

        ALTER TABLE servers ADD COLUMN IF NOT EXISTS os                text        NULL;
        ALTER TABLE servers ADD COLUMN IF NOT EXISTS listening_ports   text        NULL;
        ALTER TABLE servers ADD COLUMN IF NOT EXISTS last_discovered_at timestamptz NULL;

        CREATE TABLE IF NOT EXISTS app_config (
            key   text PRIMARY KEY,
            value text NOT NULL
        );
        """;

    // Retention policies run separately and tolerantly — a TimescaleDB version quirk must not block startup.
    private const string RetentionSql = """
        SELECT add_retention_policy('metric_samples', INTERVAL '30 days', if_not_exists => TRUE);
        SELECT add_retention_policy('healthcheck_results', INTERVAL '30 days', if_not_exists => TRUE);
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = SchemaSql;
                await command.ExecuteNonQueryAsync(cancellationToken);
                logger.LogInformation("Heimdall database schema is ready.");
                await ApplyRetentionAsync(connection, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{MaxAttempts}); retrying in 2s.", attempt, MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ApplyRetentionAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = RetentionSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Retention policies (30 days) are in place.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not apply retention policies; continuing without them.");
        }
    }
}
