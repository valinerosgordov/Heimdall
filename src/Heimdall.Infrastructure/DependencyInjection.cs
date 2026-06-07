using Dapper;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Alerting;
using Heimdall.Application.HealthChecks;
using Heimdall.Infrastructure.Alerting;
using Heimdall.Infrastructure.HealthChecks;
using Heimdall.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Heimdall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Heimdall")
            ?? throw new InvalidOperationException("Missing connection string 'Heimdall'.");

        // Map snake_case columns (created_at) to PascalCase members (CreatedAt).
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Dapper 2.x cannot bind DateOnly parameters out of the box (servers.paid_until).
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
        services.AddSingleton(dataSource);

        services.AddSingleton<IHostRepository, HostRepository>();
        services.AddSingleton<IMetricRepository, MetricRepository>();
        services.AddSingleton<IHealthCheckRepository, HealthCheckRepository>();
        services.AddSingleton<IServerRepository, ServerRepository>();
        services.AddSingleton<IOperatorStore, OperatorStore>();
        services.AddHostedService<DatabaseInitializer>();

        // Health-check probes + scheduler.
        services.AddSingleton<IHealthProbe, HttpHealthProbe>();
        services.AddSingleton<IHealthProbe, TcpHealthProbe>();
        services.AddHttpClient(HttpHealthProbe.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHostedService<HealthCheckScheduler>();

        // Alerting: rule repository, notification channels, evaluation engine.
        services.AddSingleton<IAlertRepository, AlertRepository>();
        services.AddSingleton<IAlertChannel, TelegramAlertChannel>();
        services.AddSingleton<IAlertChannel, EmailAlertChannel>();
        services.AddHttpClient(TelegramAlertChannel.HttpClientName);
        services.AddHostedService<AlertEvaluationService>();

        return services;
    }
}
