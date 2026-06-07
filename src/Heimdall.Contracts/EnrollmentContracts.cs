namespace Heimdall.Contracts;

/// <summary>An agent enrolling itself with the server (protected by the enrollment key).</summary>
public sealed record EnrollRequest
{
    public required string HostName { get; init; }
}

/// <summary>Issued once on enrollment. The agent persists <see cref="AgentKey"/> and sends it on every push.</summary>
public sealed record EnrollResponse
{
    public required string HostName { get; init; }
    public required string AgentKey { get; init; }
}
