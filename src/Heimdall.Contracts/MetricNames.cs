namespace Heimdall.Contracts;

/// <summary>Canonical metric names shared as wire vocabulary between agent and server.</summary>
public static class MetricNames
{
    /// <summary>Total CPU utilization, percent 0–100.</summary>
    public const string CpuUsage = "cpu.usage";

    /// <summary>Physical memory in use, percent 0–100.</summary>
    public const string MemoryUsage = "memory.usage";

    /// <summary>System drive space in use, percent 0–100.</summary>
    public const string DiskUsage = "disk.usage";

    /// <summary>Network bytes received per second across all interfaces.</summary>
    public const string NetRxBytesPerSec = "net.rx_bytes_per_sec";

    /// <summary>Network bytes sent per second across all interfaces.</summary>
    public const string NetTxBytesPerSec = "net.tx_bytes_per_sec";

    /// <summary>Seconds since the host booted.</summary>
    public const string UptimeSeconds = "uptime.seconds";
}
