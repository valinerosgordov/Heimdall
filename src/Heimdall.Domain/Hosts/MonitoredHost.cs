using System.Net;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Hosts;

/// <summary>A machine reporting metrics to Heimdall. Identified to operators by its unique name.</summary>
public sealed class MonitoredHost : AggregateRoot<HostId>
{
    private const int MaxNameLength = 200;

    private MonitoredHost() { } // storage rehydration

    private MonitoredHost(HostId id, string name, DateTimeOffset createdAt) : base(id)
    {
        Name = name;
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Hex SHA-256 of the host's agent key. Null until enrolled.</summary>
    public string? AgentKeyHash { get; private set; }

    public bool IsEnrolled => AgentKeyHash is not null;

    /// <summary>Validates and creates a new host. Name is trimmed and HTML-encoded.</summary>
    public static Result<MonitoredHost> Register(string? name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Host.NameEmpty", "Host name must not be empty.");

        var clean = WebUtility.HtmlEncode(name.Trim());
        if (clean.Length > MaxNameLength)
            return Error.Validation("Host.NameTooLong", $"Host name must be {MaxNameLength} characters or fewer.");

        return new MonitoredHost(HostId.New(), clean, now);
    }

    public void Touch(DateTimeOffset now) => LastSeenAt = now;

    /// <summary>Sets (or rotates) the agent key hash issued during enrollment.</summary>
    public void AssignAgentKey(string keyHash) => AgentKeyHash = keyHash;

    /// <summary>Rebuilds an aggregate from persisted state without re-running invariants.</summary>
    public static MonitoredHost FromStorage(
        HostId id,
        string name,
        DateTimeOffset createdAt,
        DateTimeOffset lastSeenAt,
        string? agentKeyHash)
        => new(id, name, createdAt) { LastSeenAt = lastSeenAt, AgentKeyHash = agentKeyHash };
}
