using Dapper;
using Heimdall.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Heimdall.IntegrationTests;

/// <summary>Spins up a real TimescaleDB container and applies the Heimdall schema once per test collection.</summary>
public sealed class TimescaleFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("timescale/timescaledb:latest-pg17")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
        await _container.StartAsync();
        DataSource = new NpgsqlDataSourceBuilder(_container.GetConnectionString()).Build();
        await new DatabaseInitializer(DataSource, NullLogger<DatabaseInitializer>.Instance)
            .StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
            await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(TimescaleCollection))]
public sealed class TimescaleCollection : ICollectionFixture<TimescaleFixture>;
