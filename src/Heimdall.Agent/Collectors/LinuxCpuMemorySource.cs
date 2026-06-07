using System.Runtime.Versioning;

namespace Heimdall.Agent.Collectors;

/// <summary>Linux CPU% (from /proc/stat deltas) and memory% (from /proc/meminfo).</summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxCpuMemorySource : ICpuMemorySource
{
    private long _lastIdle;
    private long _lastTotal;
    private bool _hasPrevious;

    public async ValueTask<(double CpuPercent, double MemoryPercent)> ReadAsync(CancellationToken cancellationToken)
    {
        var cpu = await ReadCpuUsageAsync(cancellationToken);
        var memory = await ReadMemoryUsagePercentAsync(cancellationToken);
        return (cpu, memory);
    }

    private async ValueTask<double> ReadCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (!_hasPrevious)
        {
            (_lastIdle, _lastTotal) = await SampleAsync(cancellationToken);
            _hasPrevious = true;
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        var (idle, total) = await SampleAsync(cancellationToken);
        var idleDelta = idle - _lastIdle;
        var totalDelta = total - _lastTotal;

        _lastIdle = idle;
        _lastTotal = total;

        if (totalDelta <= 0)
            return 0;

        return Math.Clamp((1.0 - ((double)idleDelta / totalDelta)) * 100.0, 0, 100);
    }

    private static async ValueTask<(long Idle, long Total)> SampleAsync(CancellationToken cancellationToken)
    {
        // First line: "cpu  user nice system idle iowait irq softirq steal guest guest_nice"
        var lines = await File.ReadAllLinesAsync("/proc/stat", cancellationToken);
        var cpuLine = lines[0];
        var fields = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        long total = 0;
        long idle = 0;
        for (var i = 1; i < fields.Length; i++)
        {
            if (!long.TryParse(fields[i], out var value))
                continue;
            total += value;
            if (i is 4 or 5) // idle + iowait
                idle += value;
        }

        return (idle, total);
    }

    private static async ValueTask<double> ReadMemoryUsagePercentAsync(CancellationToken cancellationToken)
    {
        long memTotal = 0;
        long memAvailable = 0;

        foreach (var line in await File.ReadAllLinesAsync("/proc/meminfo", cancellationToken))
        {
            if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                memTotal = ParseKilobytes(line);
            else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                memAvailable = ParseKilobytes(line);

            if (memTotal > 0 && memAvailable > 0)
                break;
        }

        if (memTotal <= 0)
            return 0;

        return Math.Clamp((1.0 - ((double)memAvailable / memTotal)) * 100.0, 0, 100);
    }

    private static long ParseKilobytes(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var value) ? value : 0;
    }
}
