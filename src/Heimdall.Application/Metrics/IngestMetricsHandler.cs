using Heimdall.Application.Abstractions;
using Heimdall.Application.Security;
using Heimdall.Contracts;
using Heimdall.Domain.Metrics;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Metrics;

/// <summary>Authenticates the host by its agent key, then stores a batch of samples. Invalid samples are skipped.</summary>
public sealed class IngestMetricsHandler(IHostRepository hosts, IMetricRepository metrics, TimeProvider clock)
{
    public async Task<Result<IngestResponse>> HandleAsync(
        MetricBatchRequest request,
        string? agentKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HostName))
            return Error.Validation("Host.NameEmpty", "Host name must not be empty.");

        var host = await hosts.GetByNameAsync(request.HostName, cancellationToken);
        if (host is null || !host.IsEnrolled)
            return Error.Unauthorized("Ingest.NotEnrolled", "Host is not enrolled. Enroll the agent first.");

        if (string.IsNullOrEmpty(agentKey) || !KeyHasher.Verify(agentKey, host.AgentKeyHash!))
            return Error.Unauthorized("Ingest.InvalidKey", "Invalid agent key.");

        if (request.Samples is null || request.Samples.Count == 0)
            return new IngestResponse { Accepted = 0, Rejected = 0 };

        var now = clock.GetUtcNow();
        var minTimestampMs = now.AddDays(-60).ToUnixTimeMilliseconds();
        var maxTimestampMs = now.AddDays(1).ToUnixTimeMilliseconds();

        var samples = new List<MetricSample>(request.Samples.Count);
        var rejected = 0;
        foreach (var dto in request.Samples)
        {
            // Reject out-of-window timestamps (also keeps FromUnixTimeMilliseconds in range — a garbage value would otherwise throw).
            if (dto.TimestampUnixMs < minTimestampMs || dto.TimestampUnixMs > maxTimestampMs)
            {
                rejected++;
                continue;
            }

            var name = MetricName.Create(dto.Metric);
            if (name.IsFailure)
            {
                rejected++;
                continue;
            }

            var sample = MetricSample.Create(host.Id, name.Value, dto.Value, DateTimeOffset.FromUnixTimeMilliseconds(dto.TimestampUnixMs));
            if (sample.IsFailure)
            {
                rejected++;
                continue;
            }

            samples.Add(sample.Value);
        }

        // Touch last-seen only when we actually stored data, so a host can't look ONLINE while storing nothing.
        if (samples.Count > 0)
        {
            await metrics.InsertSamplesAsync(samples, cancellationToken);
            await hosts.TouchAsync(host.Id, now, cancellationToken);
        }

        return new IngestResponse { Accepted = samples.Count, Rejected = rejected };
    }
}
