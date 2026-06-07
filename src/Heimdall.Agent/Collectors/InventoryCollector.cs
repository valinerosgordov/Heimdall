using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Heimdall.Contracts;

namespace Heimdall.Agent.Collectors;

/// <summary>
/// Gathers a host inventory snapshot for auto-discovery: OS, CPU cores, RAM, disk, uptime and
/// listening TCP ports — all from the BCL, no P/Invoke, cross-platform.
/// </summary>
internal static class InventoryCollector
{
    public static InventoryReportRequest Collect(string hostName) => new()
    {
        HostName = hostName,
        Os = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        CpuCores = Environment.ProcessorCount,
        RamGb = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1073741824.0, 1),
        DiskGb = ReadDiskGb(),
        UptimeSeconds = Environment.TickCount64 / 1000,
        ListeningPorts = ReadListeningPorts(),
    };

    private static double? ReadDiskGb()
    {
        try
        {
            var root = Path.GetPathRoot(AppContext.BaseDirectory) ?? "/";
            var drive = new DriveInfo(root);
            if (!drive.IsReady || drive.TotalSize <= 0)
                return null;
            return Math.Round(drive.TotalSize / 1073741824.0, 0);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadListeningPorts()
    {
        try
        {
            var ports = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(endpoint => endpoint.Port)
                .Distinct()
                .OrderBy(port => port)
                .Take(40);
            var csv = string.Join(",", ports);
            return string.IsNullOrEmpty(csv) ? null : csv;
        }
        catch
        {
            return null;
        }
    }
}
