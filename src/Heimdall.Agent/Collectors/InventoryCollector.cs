using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Heimdall.Contracts;

namespace Heimdall.Agent.Collectors;

/// <summary>
/// Gathers a host inventory snapshot for auto-discovery: OS, CPU cores, total physical RAM, disk,
/// uptime and listening TCP ports. Uses the BCL plus one kernel32 call for physical RAM on Windows
/// (and /proc/meminfo on Linux) so the reported RAM is the machine's, not the GC heap limit.
/// </summary>
internal static partial class InventoryCollector
{
    public static InventoryReportRequest Collect(string hostName) => new()
    {
        HostName = hostName,
        Os = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
        CpuCores = Environment.ProcessorCount,
        RamGb = ReadTotalRamGb(),
        DiskGb = ReadDiskGb(),
        UptimeSeconds = Environment.TickCount64 / 1000,
        ListeningPorts = ReadListeningPorts(),
    };

    private static double? ReadTotalRamGb()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
                if (GlobalMemoryStatusEx(ref status) && status.ullTotalPhys > 0)
                    return Math.Round(status.ullTotalPhys / 1073741824.0, 1);
            }
            else if (OperatingSystem.IsLinux())
            {
                foreach (var line in File.ReadLines("/proc/meminfo"))
                {
                    if (!line.StartsWith("MemTotal:", StringComparison.Ordinal))
                        continue;
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
                        return Math.Round(kb * 1024.0 / 1073741824.0, 1);
                }
            }
        }
        catch
        {
            // fall through to null
        }
        return null;
    }

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

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
