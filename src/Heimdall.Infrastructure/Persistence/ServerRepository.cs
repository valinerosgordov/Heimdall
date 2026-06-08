using Dapper;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Inventory;
using Heimdall.Domain.Inventory;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

internal sealed class ServerRepository(NpgsqlDataSource dataSource) : IServerRepository
{
    private const string Columns =
        "id, name, provider, ip_address, hostname, role, cpu_cores, ram_gb, disk_gb, location, " +
        "monthly_cost, currency, paid_until, user_count, notes, linked_healthcheck_id, linked_host_name, " +
        "billing_cycle, auto_renew, payment_method, " +
        "os, listening_ports, last_discovered_at, created_at, updated_at";

    public async Task AddAsync(Server server, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO servers (id, name, provider, ip_address, hostname, role, cpu_cores, ram_gb, disk_gb, location,
                                 monthly_cost, currency, paid_until, user_count, notes, linked_healthcheck_id, linked_host_name,
                                 billing_cycle, auto_renew, payment_method,
                                 os, listening_ports, last_discovered_at, created_at, updated_at)
            VALUES (@id, @name, @provider, @ipAddress, @hostname, @role, @cpuCores, @ramGb, @diskGb, @location,
                    @monthlyCost, @currency, @paidUntil, @userCount, @notes, @linkedHealthCheckId, @linkedHostName,
                    @billingCycle, @autoRenew, @paymentMethod,
                    @os, @listeningPorts, @lastDiscoveredAt, @createdAt, @updatedAt);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(server), cancellationToken: cancellationToken));
    }

    public async Task<Server?> GetAsync(ServerId id, CancellationToken cancellationToken)
    {
        var sql = $"SELECT {Columns} FROM servers WHERE id = @id;";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ServerRow>(new CommandDefinition(
            sql, new { id = id.Value }, cancellationToken: cancellationToken));
        return row is null ? null : ToEntity(row);
    }

    public async Task<Server?> FindByHostNameAsync(string hostName, CancellationToken cancellationToken)
    {
        var sql =
            $"""
            SELECT {Columns} FROM servers
            WHERE LOWER(linked_host_name) = LOWER(@h) OR LOWER(name) = LOWER(@h)
            ORDER BY (LOWER(linked_host_name) = LOWER(@h)) DESC
            LIMIT 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ServerRow>(new CommandDefinition(
            sql, new { h = hostName }, cancellationToken: cancellationToken));
        return row is null ? null : ToEntity(row);
    }

    public async Task UpsertDiscoveredAsync(Server server, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO servers (id, name, provider, ip_address, hostname, role, cpu_cores, ram_gb, disk_gb, location,
                                 monthly_cost, currency, paid_until, user_count, notes, linked_healthcheck_id, linked_host_name,
                                 os, listening_ports, last_discovered_at, created_at, updated_at)
            VALUES (@id, @name, @provider, @ipAddress, @hostname, @role, @cpuCores, @ramGb, @diskGb, @location,
                    @monthlyCost, @currency, @paidUntil, @userCount, @notes, @linkedHealthCheckId, @linkedHostName,
                    @os, @listeningPorts, @lastDiscoveredAt, @createdAt, @updatedAt)
            ON CONFLICT (linked_host_name) WHERE linked_host_name IS NOT NULL
            DO UPDATE SET os = excluded.os, cpu_cores = excluded.cpu_cores, ram_gb = excluded.ram_gb,
                          disk_gb = excluded.disk_gb, hostname = excluded.hostname,
                          listening_ports = excluded.listening_ports, last_discovered_at = excluded.last_discovered_at,
                          updated_at = excluded.updated_at;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(server), cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Server server, CancellationToken cancellationToken)
    {
        const string sql =
            """
            UPDATE servers SET
                name = @name, provider = @provider, ip_address = @ipAddress, hostname = @hostname, role = @role,
                cpu_cores = @cpuCores, ram_gb = @ramGb, disk_gb = @diskGb, location = @location,
                monthly_cost = @monthlyCost, currency = @currency, paid_until = @paidUntil, user_count = @userCount,
                notes = @notes, linked_healthcheck_id = @linkedHealthCheckId, linked_host_name = @linkedHostName,
                billing_cycle = @billingCycle, auto_renew = @autoRenew, payment_method = @paymentMethod,
                os = @os, listening_ports = @listeningPorts, last_discovered_at = @lastDiscoveredAt,
                updated_at = @updatedAt
            WHERE id = @id;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, ToParams(server), cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<IReadOnlyList<ServerRecord>> ListWithStatusAsync(CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT s.id, s.name, s.provider, s.ip_address, s.hostname, s.role, s.cpu_cores, s.ram_gb, s.disk_gb, s.location,
                   s.monthly_cost, s.currency, s.paid_until, s.user_count, s.notes, s.linked_healthcheck_id, s.linked_host_name,
                   s.billing_cycle, s.auto_renew, s.payment_method,
                   s.os, s.listening_ports, s.last_discovered_at,
                   r.is_up
            FROM servers s
            LEFT JOIN LATERAL (
                SELECT is_up
                FROM healthcheck_results r
                WHERE r.target_id = s.linked_healthcheck_id
                ORDER BY time DESC
                LIMIT 1
            ) r ON s.linked_healthcheck_id IS NOT NULL
            ORDER BY s.paid_until NULLS LAST, s.name;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StatusRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));

        return
        [
            .. rows.Select(r => new ServerRecord(
                r.Id, r.Name, r.Provider, r.IpAddress, r.Hostname, r.Role,
                r.CpuCores, r.RamGb, r.DiskGb, r.Location, r.MonthlyCost, r.Currency, r.PaidUntil, r.UserCount, r.Notes,
                r.LinkedHealthCheckId, r.LinkedHostName,
                r.Os, r.ListeningPorts, AsUtc(r.LastDiscoveredAt),
                r.IsUp,
                r.BillingCycle, r.AutoRenew, r.PaymentMethod)),
        ];
    }

    public async Task<bool> DeleteAsync(ServerId id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM server_links WHERE from_id = @id OR to_id = @id;",
            new { id = id.Value }, transaction, cancellationToken: cancellationToken));

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM servers WHERE id = @id;",
            new { id = id.Value }, transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return affected > 0;
    }

    public async Task AddLinkAsync(ServerLink link, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO server_links (id, from_id, to_id, kind) VALUES (@id, @fromId, @toId, @kind);",
            new { id = link.Id, fromId = link.From.Value, toId = link.To.Value, kind = link.Kind },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteLinkAsync(Guid linkId, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM server_links WHERE id = @id;",
            new { id = linkId },
            cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<IReadOnlyList<ServerLink>> ListLinksAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<LinkRow>(new CommandDefinition(
            "SELECT id, from_id, to_id, kind FROM server_links;",
            cancellationToken: cancellationToken));

        return [.. rows.Select(r => ServerLink.FromStorage(r.Id, r.FromId, r.ToId, r.Kind))];
    }

    private static object ToParams(Server s) => new
    {
        id = s.Id.Value,
        name = s.Name,
        provider = s.Provider,
        ipAddress = s.IpAddress,
        hostname = s.Hostname,
        role = s.Role,
        cpuCores = s.CpuCores,
        ramGb = s.RamGb,
        diskGb = s.DiskGb,
        location = s.Location,
        monthlyCost = s.MonthlyCost,
        currency = s.Currency,
        paidUntil = s.PaidUntil,
        userCount = s.UserCount,
        notes = s.Notes,
        linkedHealthCheckId = s.LinkedHealthCheckId,
        linkedHostName = s.LinkedHostName,
        billingCycle = s.BillingCycle,
        autoRenew = s.AutoRenew,
        paymentMethod = s.PaymentMethod,
        os = s.Os,
        listeningPorts = s.ListeningPorts,
        lastDiscoveredAt = s.LastDiscoveredAt?.UtcDateTime,
        createdAt = s.CreatedAt.UtcDateTime,
        updatedAt = s.UpdatedAt.UtcDateTime,
    };

    private static Server ToEntity(ServerRow r) => Server.FromStorage(
        new ServerId(r.Id), r.Name, r.Provider, r.IpAddress, r.Hostname, r.Role,
        r.CpuCores, r.RamGb, r.DiskGb, r.Location, r.MonthlyCost, r.Currency, r.PaidUntil, r.UserCount, r.Notes,
        r.LinkedHealthCheckId, r.LinkedHostName,
        r.Os, r.ListeningPorts, AsUtc(r.LastDiscoveredAt),
        AsUtc(r.CreatedAt), AsUtc(r.UpdatedAt),
        r.BillingCycle, r.AutoRenew, r.PaymentMethod);

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static DateTimeOffset? AsUtc(DateTime? value)
        => value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));

    private sealed record ServerRow(
        Guid Id, string Name, string? Provider, string? IpAddress, string? Hostname, string? Role,
        int? CpuCores, double? RamGb, double? DiskGb, string? Location, decimal? MonthlyCost, string? Currency,
        DateOnly? PaidUntil, int? UserCount, string? Notes, Guid? LinkedHealthCheckId, string? LinkedHostName,
        string? BillingCycle, bool AutoRenew, string? PaymentMethod,
        string? Os, string? ListeningPorts, DateTime? LastDiscoveredAt,
        DateTime CreatedAt, DateTime UpdatedAt);

    private sealed record StatusRow(
        Guid Id, string Name, string? Provider, string? IpAddress, string? Hostname, string? Role,
        int? CpuCores, double? RamGb, double? DiskGb, string? Location, decimal? MonthlyCost, string? Currency,
        DateOnly? PaidUntil, int? UserCount, string? Notes, Guid? LinkedHealthCheckId, string? LinkedHostName,
        string? BillingCycle, bool AutoRenew, string? PaymentMethod,
        string? Os, string? ListeningPorts, DateTime? LastDiscoveredAt, bool? IsUp);

    private sealed record LinkRow(Guid Id, Guid FromId, Guid ToId, string Kind);
}
