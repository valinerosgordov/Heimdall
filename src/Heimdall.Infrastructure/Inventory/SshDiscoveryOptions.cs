namespace Heimdall.Infrastructure.Inventory;

/// <summary>
/// Configures scheduled read-only SSH discovery. Targets live in config (gitignored appsettings),
/// never in the database or the repo — only a path to a local private key is referenced, never the key.
/// </summary>
public sealed class SshDiscoveryOptions
{
    public const string Section = "Heimdall:SshDiscovery";

    public int IntervalMinutes { get; set; } = 30;

    public List<SshTarget> Servers { get; set; } = [];
}

public sealed class SshTarget
{
    /// <summary>Must match an inventory server's name so discovery enriches that card.</summary>
    public string Name { get; set; } = string.Empty;

    public string Host { get; set; } = string.Empty;
    public string User { get; set; } = "root";
    public int Port { get; set; } = 22;

    /// <summary>Path to a local private key file (e.g. ~/.ssh/id_ed25519). The key material is never stored.</summary>
    public string KeyPath { get; set; } = string.Empty;
}
