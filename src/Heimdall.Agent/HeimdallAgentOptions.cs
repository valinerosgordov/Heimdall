namespace Heimdall.Agent;

/// <summary>Agent configuration, bound from the "Heimdall" section.</summary>
public sealed class HeimdallAgentOptions
{
    public const string SectionName = "Heimdall";

    /// <summary>Base URL of the Heimdall API.</summary>
    public string ServerUrl { get; init; } = "http://localhost:5087";

    /// <summary>Bootstrap enrollment key, used once to obtain a per-host agent key.</summary>
    public string EnrollmentKey { get; init; } = string.Empty;

    /// <summary>Identifies this host to the server. Defaults to the machine name.</summary>
    public string HostName { get; init; } = Environment.MachineName;

    /// <summary>How often to collect and push, in seconds.</summary>
    public int IntervalSeconds { get; init; } = 5;
}
