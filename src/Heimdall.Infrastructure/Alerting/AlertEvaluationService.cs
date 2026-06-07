using Heimdall.Application.Abstractions;
using Heimdall.Application.Alerting;
using Heimdall.Application.Metrics;
using Heimdall.Domain.Alerting;
using Heimdall.Domain.Hosts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Heimdall.Infrastructure.Alerting;

/// <summary>Evaluates enabled rules per host each tick: fires when a metric breaches for the whole duration window, resolves when it clears.</summary>
internal sealed class AlertEvaluationService(
    IAlertRepository alerts,
    IHostRepository hosts,
    IMetricRepository metrics,
    IEnumerable<IAlertChannel> channels,
    TimeProvider clock,
    ILogger<AlertEvaluationService> logger) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(15);
    private readonly IAlertChannel[] _channels = channels.Where(c => c.IsConfigured).ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Alert evaluator started ({ChannelCount} notification channels configured).", _channels.Length);

        using var timer = new PeriodicTimer(Tick);
        do
        {
            try
            {
                await EvaluateAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alert evaluation tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EvaluateAsync(CancellationToken cancellationToken)
    {
        var rules = await alerts.GetEnabledRulesAsync(cancellationToken);
        if (rules.Count == 0)
            return;

        var hostList = await hosts.ListAsync(cancellationToken);
        var now = clock.GetUtcNow();

        foreach (var rule in rules)
        {
            var targets = rule.HostName is null ? hostList : hostList.Where(h => h.Name == rule.HostName);
            foreach (var host in targets)
                await EvaluateRuleForHostAsync(rule, host.Id, host.Name, now, cancellationToken);
        }
    }

    private async Task EvaluateRuleForHostAsync(AlertRule rule, Guid hostId, string hostName, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var from = now.AddSeconds(-rule.DurationSeconds);
        var points = await metrics.QueryAsync(new HostId(hostId), rule.Metric, from, now, cancellationToken);

        var breached = IsSustainedBreach(rule, points);
        var active = await alerts.GetActiveAlertAsync(rule.Id, hostName, cancellationToken);

        if (breached && active is null)
        {
            var alert = Alert.Fire(rule, hostName, points[^1].Value, now);
            await alerts.InsertAlertAsync(alert, cancellationToken);
            logger.LogInformation("Alert FIRING: {Rule} on {Host} ({Metric}={Value:0.##})", rule.Name, hostName, rule.Metric, alert.Value);
            await NotifyAsync(new AlertNotification(rule.Name, hostName, rule.Metric, rule.Severity, alert.Value, Resolved: false, now), cancellationToken);
        }
        else if (!breached && active is not null)
        {
            await alerts.ResolveAlertAsync(active.Id, now, cancellationToken);
            logger.LogInformation("Alert RESOLVED: {Rule} on {Host}", rule.Name, hostName);
            await NotifyAsync(new AlertNotification(rule.Name, hostName, rule.Metric, rule.Severity, active.Value, Resolved: true, now), cancellationToken);
        }
    }

    // A single transient sample must not satisfy a "sustained for DurationSeconds" rule:
    // require every sample to breach AND the samples to span at least half the duration window.
    private static bool IsSustainedBreach(AlertRule rule, IReadOnlyList<MetricPoint> points)
    {
        if (points.Count == 0 || !points.All(p => rule.IsBreached(p.Value)))
            return false;

        var spanMs = points[^1].TimestampUnixMs - points[0].TimestampUnixMs;
        return spanMs >= rule.DurationSeconds * 1000L / 2;
    }

    private async Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
    {
        foreach (var channel in _channels)
        {
            try
            {
                await channel.NotifyAsync(notification, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Alert channel {Channel} failed.", channel.Name);
            }
        }
    }
}
