using Heimdall.Contracts;

namespace Heimdall.Application.Alerting;

/// <summary>Lists each notification channel and whether it is configured.</summary>
public sealed class GetChannelStatusHandler(IEnumerable<IAlertChannel> channels)
{
    public IReadOnlyList<ChannelStatusDto> Handle()
        => [.. channels.Select(c => new ChannelStatusDto { Name = c.Name, Configured = c.IsConfigured })];
}
