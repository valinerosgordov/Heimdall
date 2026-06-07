using Dapper;
using Heimdall.Application.Abstractions;
using Npgsql;

namespace Heimdall.Infrastructure.Persistence;

/// <summary>Operator account stored as rows in the app_config key/value table.</summary>
internal sealed class OperatorStore(NpgsqlDataSource dataSource) : IOperatorStore
{
    private const string UsernameKey = "operator.username";
    private const string PasswordKey = "operator.password_sha256";

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

    public async Task SetAsync(string username, string passwordSha256, CancellationToken cancellationToken)
    {
        const string sql =
            """
            INSERT INTO app_config (key, value) VALUES (@key, @value)
            ON CONFLICT (key) DO UPDATE SET value = excluded.value;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new[]
            {
                new { key = UsernameKey, value = username },
                new { key = PasswordKey, value = passwordSha256 },
            },
            cancellationToken: cancellationToken));
    }

    private sealed record AppConfigRow(string Key, string Value);
}
