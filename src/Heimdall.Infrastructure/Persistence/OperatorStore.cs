using Dapper;
using Heimdall.Application.Abstractions;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

/// <summary>Operator account stored as rows in the app_config key/value table.</summary>
internal sealed class OperatorStore(NpgsqlDataSource dataSource) : IOperatorStore
{
    private const string UsernameKey = "operator.username";
    private const string PasswordKey = "operator.password_hash";

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
        => await GetAsync(cancellationToken) is not null;

    public async Task<OperatorCredentials?> GetAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<AppConfigRow>(new CommandDefinition(
            "SELECT key, value FROM app_config WHERE key IN (@u, @p);",
            new { u = UsernameKey, p = PasswordKey },
            cancellationToken: cancellationToken));

        var map = rows.ToDictionary(r => r.Key, r => r.Value);
        return map.TryGetValue(UsernameKey, out var username) && map.TryGetValue(PasswordKey, out var hash)
            ? new OperatorCredentials(username, hash)
            : null;
    }

    public async Task<bool> TryInitializeAsync(string username, string passwordHash, CancellationToken cancellationToken)
    {
        // Conditional insert in one transaction: the app_config PRIMARY KEY on `key` serializes concurrent
        // first-run setups, and DO NOTHING means a second writer creates nothing (returns false) instead of
        // overwriting the first operator.
        const string sql =
            """
            INSERT INTO app_config (key, value) VALUES (@key, @value)
            ON CONFLICT (key) DO NOTHING;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var insertedUser = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { key = UsernameKey, value = username }, transaction, cancellationToken: cancellationToken));
        var insertedPassword = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { key = PasswordKey, value = passwordHash }, transaction, cancellationToken: cancellationToken));

        if (insertedUser == 0 || insertedPassword == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private sealed record AppConfigRow(string Key, string Value);
}
