using System.Net;
using System.Net.Mail;
using Heimdall.Application.Alerting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Heimdall.Infrastructure.Alerting;

/// <summary>Sends alerts over SMTP. No-op unless Host + From + To are configured.</summary>
internal sealed class EmailAlertChannel(IConfiguration configuration, ILogger<EmailAlertChannel> logger) : IAlertChannel
{
    public string Name => "Email";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Heimdall:Email:Host"]) &&
        !string.IsNullOrWhiteSpace(configuration["Heimdall:Email:From"]) &&
        !string.IsNullOrWhiteSpace(configuration["Heimdall:Email:To"]);

    public async Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return;

        try
        {
            var host = configuration["Heimdall:Email:Host"]!;
            var port = int.TryParse(configuration["Heimdall:Email:Port"], out var p) ? p : 587;

            using var client = new SmtpClient(host, port);
            var user = configuration["Heimdall:Email:User"];
            if (!string.IsNullOrWhiteSpace(user))
            {
                client.Credentials = new NetworkCredential(user, configuration["Heimdall:Email:Password"]);
                client.EnableSsl = true;
            }

            var state = notification.Resolved ? "RESOLVED" : notification.Severity.ToString().ToUpperInvariant();
            using var message = new MailMessage(
                configuration["Heimdall:Email:From"]!,
                configuration["Heimdall:Email:To"]!,
                $"[Heimdall] {state}: {notification.RuleName}",
                $"Host: {notification.HostName}\nMetric: {notification.Metric}\nValue: {notification.Value:0.##}\nAt: {notification.At:u}");

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Email notify failed.");
        }
    }
}
