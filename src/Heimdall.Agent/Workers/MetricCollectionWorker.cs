using Heimdall.Agent.Collectors;
using Heimdall.Agent.Enrollment;
using Heimdall.Agent.Push;
using Heimdall.Contracts;
using Microsoft.Extensions.Options;

namespace Heimdall.Agent.Workers;

/// <summary>
/// Ensures enrollment, then collects and pushes the full metric set on a fixed cadence.
/// Batches are buffered (bounded) and resent oldest-first, so a server restart or network blip
/// doesn't lose data.
/// </summary>
internal sealed class MetricCollectionWorker(
    ISystemMetricsCollector collector,
    AgentKeyStore keyStore,
    HeimdallApiClient apiClient,
    IOptions<HeimdallAgentOptions> options,
    TimeProvider clock,
    ILogger<MetricCollectionWorker> logger) : BackgroundService
{
    // ~10 min of backlog at a 5s cadence; oldest batches are dropped beyond this.
    private const int MaxPendingBatches = 120;

    private readonly HeimdallAgentOptions _options = options.Value;
    private readonly Queue<MetricBatchRequest> _pending = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
        logger.LogInformation(
            "Heimdall agent started for host {HostName}; pushing to {ServerUrl} every {IntervalSeconds}s.",
            _options.HostName, _options.ServerUrl, interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                // Always collect and buffer, even while offline/unenrolled, so the backlog fills.
                var readings = await collector.CollectAsync(stoppingToken);
                var timestamp = clock.GetUtcNow().ToUnixTimeMilliseconds();
                Enqueue(new MetricBatchRequest
                {
                    HostName = _options.HostName,
                    Samples =
                    [
                        .. readings.Select(r => new MetricSampleDto
                        {
                            Metric = r.Metric,
                            Value = r.Value,
                            TimestampUnixMs = timestamp,
                        }),
                    ],
                });

                var agentKey = await keyStore.EnsureKeyAsync(stoppingToken);
                if (agentKey is null)
                {
                    logger.LogWarning("Agent not enrolled yet; buffered {Count} batch(es).", _pending.Count);
                    continue;
                }

                await FlushAsync(agentKey, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Metric collection tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private void Enqueue(MetricBatchRequest batch)
    {
        if (_pending.Count >= MaxPendingBatches)
            _pending.Dequeue(); // drop oldest to stay bounded
        _pending.Enqueue(batch);
    }

    private async Task FlushAsync(string agentKey, CancellationToken cancellationToken)
    {
        while (_pending.Count > 0)
        {
            var outcome = await apiClient.PushAsync(_pending.Peek(), agentKey, cancellationToken);
            if (outcome == PushOutcome.Success)
            {
                _pending.Dequeue();
            }
            else if (outcome == PushOutcome.Unauthorized)
            {
                logger.LogWarning("Agent key was rejected; re-enrolling next tick ({Count} buffered).", _pending.Count);
                keyStore.Invalidate();
                return;
            }
            else
            {
                logger.LogWarning("Push failed; {Count} batch(es) buffered for resend.", _pending.Count);
                return;
            }
        }
    }
}
