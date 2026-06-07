using System.Net.NetworkInformation;
using Heimdall.Contracts;

namespace Heimdall.Agent.Collectors;

/// <summary>
/// Composes platform CPU/memory with cross-platform disk, network throughput and uptime
/// (DriveInfo / NetworkInterface / Environment.TickCount64 — all BCL, no P/Invoke).
/// </summary>
internal sealed class SystemMetricsCollector(ICpuMemorySource cpuMemory, TimeProvider clock) : ISystemMetricsCollector
{
    private long _lastNetTimestamp;
    private long _lastBytesReceived;
    private long _lastBytesSent;
    private bool _hasNetBaseline;

    public async ValueTask<IReadOnlyList<MetricReading>> CollectAsync(CancellationToken cancellationToken)
    {
        var (cpu, memory) = await cpuMemory.ReadAsync(cancellationToken);
        var (rx, tx) = ReadNetworkThroughput();

        return
        [
            new MetricReading(MetricNames.CpuUsage, cpu),
            new MetricReading(MetricNames.MemoryUsage, memory),
            new MetricReading(MetricNames.DiskUsage, ReadDiskUsagePercent()),
            new MetricReading(MetricNames.NetRxBytesPerSec, rx),
            new MetricReading(MetricNames.NetTxBytesPerSec, tx),
            new MetricReading(MetricNames.UptimeSeconds, Environment.TickCount64 / 1000.0),
        ];
    }

    private (double Rx, double Tx) ReadNetworkThroughput()
    {
        long received = 0;
        long sent = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            var stats = nic.GetIPStatistics();
            received += stats.BytesReceived;
            sent += stats.BytesSent;
        }

        var now = clock.GetTimestamp();
        if (!_hasNetBaseline)
        {
            _lastBytesReceived = received;
            _lastBytesSent = sent;
            _lastNetTimestamp = now;
            _hasNetBaseline = true;
            return (0, 0);
        }

        var elapsedSeconds = clock.GetElapsedTime(_lastNetTimestamp, now).TotalSeconds;
        var rx = elapsedSeconds > 0 ? Math.Max(0, (received - _lastBytesReceived) / elapsedSeconds) : 0;
        var tx = elapsedSeconds > 0 ? Math.Max(0, (sent - _lastBytesSent) / elapsedSeconds) : 0;

        _lastBytesReceived = received;
        _lastBytesSent = sent;
        _lastNetTimestamp = now;
        return (rx, tx);
    }

    private static double ReadDiskUsagePercent()
    {
        try
        {
            var root = Path.GetPathRoot(AppContext.BaseDirectory) ?? "/";
            var drive = new DriveInfo(root);
            if (!drive.IsReady || drive.TotalSize <= 0)
                return 0;

            var used = drive.TotalSize - drive.TotalFreeSpace;
            return Math.Clamp((double)used / drive.TotalSize * 100.0, 0, 100);
        }
        catch
        {
            return 0;
        }
    }
}
