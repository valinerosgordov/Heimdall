using Dapper;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Hosts;
using Heimdall.Domain.Hosts;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

internal sealed class HostRepository(NpgsqlDataSource dataSource) : IHostRepository
{
    public async Task<MonitoredHost?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT id, name, created_at, last_seen_at, agent_key_hash
            FROM hosts
            WHERE name = @name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<HostRow>(
            new CommandDefinition(sql, new { name }, cancellationToken: cancellationToken));

        return row is null
            ? null
            : MonitoredHost.FromStorage(
                new HostId(row.Id), row.Name, AsUtc(row.CreatedAt), AsUtc(row.LastSeenAt), row.AgentKeyHash);
    }

    public async Task EnrollAsync(MonitoredHost host, DateTimeOffset now, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO hosts (id, name, created_at, last_seen_at, agent_key_hash)
            VALUES (@id, @name, @now, @now, @agentKeyHash)
            ON CONFLICT (name) DO UPDATE
                SET agent_key_hash = EXCLUDED.agent_key_hash,
                    last_seen_at   = EXCLUDED.last_seen_at;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { id = host.Id.Value, name = host.Name, now = now.UtcDateTime, agentKeyHash = host.AgentKeyHash },
            cancellationToken: cancellationToken));
    }

    public async Task TouchAsync(HostId hostId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE hosts SET last_seen_at = @now WHERE id = @id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { id = hostId.Value, now = now.UtcDateTime }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<HostSummary>> ListAsync(CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT id, name, created_at, last_seen_at, agent_key_hash
            FROM hosts
            ORDER BY name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<HostRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));

        return [.. rows.Select(r => new HostSummary(r.Id, r.Name, AsUtc(r.CreatedAt), AsUtc(r.LastSeenAt)))];
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed record HostRow(Guid Id, string Name, DateTime CreatedAt, DateTime LastSeenAt, string? AgentKeyHash);
}
