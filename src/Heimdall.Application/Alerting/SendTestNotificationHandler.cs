using Heimdall.Contracts;
using Heimdall.Domain.Alerting;

namespace Heimdall.Application.Alerting;

/// <summary>Sends a sample notification through every configured channel so the operator can verify delivery.</summary>
public sealed class SendTestNotificationHandler(IEnumerable<IAlertChannel> channels, TimeProvider clock)
{
    public async Task<TestNotificationResult> HandleAsync(CancellationToken cancellationToken)
    {
        var configured = channels.Where(c => c.IsConfigured).ToArray();
        var notification = new AlertNotification(
            RuleName: "Heimdall test notification",
            HostName: "(test)",
            Metric: "test",
            Severity: AlertSeverity.Warning,
            Value: 1,
            Resolved: false,
            At: clock.GetUtcNow());

        foreach (var channel in configured)
        {
            try
            {
                await channel.NotifyAsync(notification, cancellationToken);
            }
            catch
            {
                // A single channel's failure shouldn't fail the whole test.
            }
        }

        return new TestNotificationResult
        {
            ChannelsNotified = configured.Length,
            Channels = [.. configured.Select(c => c.Name)],
        };
    }
}
