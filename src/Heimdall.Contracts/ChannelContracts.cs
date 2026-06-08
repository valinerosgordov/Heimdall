namespace Heimdall.Contracts;

/// <summary>Whether a notification channel (Telegram, Email) is configured and ready.</summary>
public sealed record ChannelStatusDto
{
    public required string Name { get; init; }
    public required bool Configured { get; init; }
}

/// <summary>Outcome of sending a test notification through the configured channels.</summary>
public sealed record TestNotificationResult
{
    public required int ChannelsNotified { get; init; }
    public required IReadOnlyList<string> Channels { get; init; }
}
