using Dapper;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Metrics;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.Metrics;
using Npgsql;
using NpgsqlTypes;

namespace Heimdall.Infrastructure.Persistence;

internal sealed class MetricRepository(NpgsqlDataSource dataSource) : IMetricRepository
{
    public async Task InsertSamplesAsync(IReadOnlyList<MetricSample> samples, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var writer = await connection.BeginBinaryImportAsync(
            "COPY metric_samples (time, host_id, metric, value) FROM STDIN (FORMAT BINARY)",
            cancellationToken);

        foreach (var sample in samples)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(sample.Timestamp.UtcDateTime, NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(sample.HostId.Value, NpgsqlDbType.Uuid, cancellationToken);
            await writer.WriteAsync(sample.Metric.Value, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(sample.Value, NpgsqlDbType.Double, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MetricPoint>> QueryAsync(
        HostId hostId,
        string metric,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT (extract(epoch FROM time) * 1000)::bigint AS timestamp_unix_ms,
                   value
            FROM metric_samples
            WHERE host_id = @hostId AND metric = @metric AND time >= @from AND time <= @to
            ORDER BY time;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<MetricPoint>(new CommandDefinition(
            sql,
            new { hostId = hostId.Value, metric, from = from.UtcDateTime, to = to.UtcDateTime },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<MetricPoint>> QueryBucketedAsync(
        HostId hostId,
        string metric,
        DateTimeOffset from,
        DateTimeOffset to,
        int bucketSeconds,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT (extract(epoch FROM time_bucket(make_interval(secs => @bucketSeconds), time)) * 1000)::bigint AS timestamp_unix_ms,
                   avg(value) AS value
            FROM metric_samples
            WHERE host_id = @hostId AND metric = @metric AND time >= @from AND time <= @to
            GROUP BY 1
            ORDER BY 1;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<MetricPoint>(new CommandDefinition(
            sql,
            new { hostId = hostId.Value, metric, from = from.UtcDateTime, to = to.UtcDateTime, bucketSeconds },
            cancellationToken: cancellationToken));

        return [.. rows];
    }

    public async Task<IReadOnlyList<LatestMetric>> GetLatestPerHostAsync(
        IReadOnlyList<string> metrics,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT DISTINCT ON (host_id, metric) host_id, metric, value
            FROM metric_samples
            WHERE metric = ANY(@metrics)
            ORDER BY host_id, metric, time DESC;
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<LatestMetric>(new CommandDefinition(
            sql,
            new { metrics = metrics.ToArray() },
            cancellationToken: cancellationToken));

        return [.. rows];
    }
}
