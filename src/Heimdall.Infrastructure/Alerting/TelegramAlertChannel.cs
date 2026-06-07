using System.Net.Http.Json;
using Heimdall.Application.Alerting;
using Heimdall.Domain.Alerting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Heimdall.Infrastructure.Alerting;

/// <summary>Sends alerts to a Telegram chat via the Bot API. No-op unless BotToken + ChatId are configured.</summary>
internal sealed class TelegramAlertChannel(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TelegramAlertChannel> logger) : IAlertChannel
{
    public const string HttpClientName = "alerts";

    private string? Token => configuration["Heimdall:Telegram:BotToken"];
    private string? ChatId => configuration["Heimdall:Telegram:ChatId"];

    public string Name => "Telegram";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(ChatId);

    public async Task NotifyAsync(AlertNotification notification, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            return;

        try
        {
            var prefix = notification.Resolved
                ? "✅ RESOLVED"
                : notification.Severity == AlertSeverity.Critical ? "🔴 CRITICAL" : "🟡 WARNING";
            var text = $"{prefix}: {notification.RuleName}\nHost: {notification.HostName}\n{notification.Metric} = {notification.Value:0.##}";

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(
                $"https://api.telegram.org/bot{Token}/sendMessage",
                new { chat_id = ChatId, text },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Telegram notify returned status {StatusCode}.", (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Telegram notify failed.");
        }
    }
}
