using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Heimdall.Agent.Collectors;

/// <summary>Windows CPU% (GetSystemTimes deltas) and memory load% (GlobalMemoryStatusEx). kernel32 P/Invoke only.</summary>
[SupportedOSPlatform("windows")]
internal sealed partial class WindowsCpuMemorySource : ICpuMemorySource
{
    private long _lastIdle;
    private long _lastKernel;
    private long _lastUser;
    private bool _hasPrevious;

    public async ValueTask<(double CpuPercent, double MemoryPercent)> ReadAsync(CancellationToken cancellationToken)
    {
        var cpu = await ReadCpuUsageAsync(cancellationToken);
        return (cpu, ReadMemoryUsagePercent());
    }

    private async ValueTask<double> ReadCpuUsageAsync(CancellationToken cancellationToken)
    {
        if (!_hasPrevious)
        {
            Sample(out _lastIdle, out _lastKernel, out _lastUser);
            _hasPrevious = true;
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        Sample(out var idle, out var kernel, out var user);

        var idleDelta = idle - _lastIdle;
        var totalDelta = (kernel - _lastKernel) + (user - _lastUser); // kernel time includes idle time

        _lastIdle = idle;
        _lastKernel = kernel;
        _lastUser = user;

        if (totalDelta <= 0)
            return 0;

        return Math.Clamp((1.0 - ((double)idleDelta / totalDelta)) * 100.0, 0, 100);
    }

    private static void Sample(out long idle, out long kernel, out long user)
    {
        if (!GetSystemTimes(out idle, out kernel, out user))
            throw new InvalidOperationException($"GetSystemTimes failed (Win32 error {Marshal.GetLastWin32Error()}).");
    }

    private static double ReadMemoryUsagePercent()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
            throw new InvalidOperationException($"GlobalMemoryStatusEx failed (Win32 error {Marshal.GetLastWin32Error()}).");

        return status.MemoryLoad;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }
}
