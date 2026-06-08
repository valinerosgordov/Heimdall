using Heimdall.Application.Abstractions;
using Heimdall.Application.Alerting;
using Heimdall.Domain.Alerting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Heimdall.Infrastructure.Inventory;

/// <summary>
/// Sends a renewal reminder through the configured alert channels when a server's paid-until date is within
/// the threshold (or overdue). Dedupes per (server, due-date) in memory so it notifies at most once per date.
/// Idle (and cheap) when no notification channels are configured.
/// </summary>
internal sealed class BillingReminderService(
    IServerRepository servers,
    IEnumerable<IAlertChannel> channels,
    TimeProvider clock,
    ILogger<BillingReminderService> logger) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromHours(6);
    private const int ReminderThresholdDays = 7;

    private readonly IAlertChannel[] _channels = channels.Where(c => c.IsConfigured).ToArray();
    private readonly Dictionary<Guid, DateOnly> _remindedFor = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channels.Length == 0)
        {
            logger.LogInformation("Billing reminders idle — no notification channels configured.");
            return;
        }

        logger.LogInformation("Billing reminder service started ({ChannelCount} channels).", _channels.Length);

        using var timer = new PeriodicTimer(Tick);
        do
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Billing reminder tick failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var serverList = await servers.ListWithStatusAsync(cancellationToken);

        foreach (var server in serverList)
        {
            if (server.PaidUntil is not { } due)
                continue;

            var daysLeft = due.DayNumber - today.DayNumber;
            if (daysLeft > ReminderThresholdDays)
                continue;

            // Already reminded for this exact due-date? (Re-reminds once the date is bumped on renewal.)
            if (_remindedFor.TryGetValue(server.Id, out var last) && last == due)
                continue;

            var severity = daysLeft <= 0 ? AlertSeverity.Critical : AlertSeverity.Warning;
            var notification = new AlertNotification(
                RuleName: "Server renewal due",
                HostName: server.Name,
                Metric: "days left",
                Severity: severity,
                Value: daysLeft,
                Resolved: false,
                At: now);

            foreach (var channel in _channels)
            {
                try
                {
                    await channel.NotifyAsync(notification, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Renewal reminder channel {Channel} failed.", channel.Name);
                }
            }

            _remindedFor[server.Id] = due;
            logger.LogInformation("Renewal reminder sent for {Server} ({DaysLeft}d).", server.Name, daysLeft);
        }
    }
}
