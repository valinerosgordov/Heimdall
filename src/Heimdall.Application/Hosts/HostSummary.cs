namespace Heimdall.Application.Hosts;

/// <summary>Read model for the host overview.</summary>
public readonly record struct HostSummary(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset LastSeenAt);
