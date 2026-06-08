using System.Globalization;
using Heimdall.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace Heimdall.Infrastructure.Inventory;

/// <summary>
/// Periodically SSHes (read-only) into the configured servers and refreshes their auto-discovered
/// inventory fields (OS, CPU, RAM, disk, listening ports). Manual fields (cost, notes, role, links) are
/// preserved. Targets + key paths come from config, never the DB. Idle when nothing is configured.
/// </summary>
internal sealed class SshDiscoveryService(
    IServerRepository servers,
    IOptions<SshDiscoveryOptions> options,
    TimeProvider clock,
    ILogger<SshDiscoveryService> logger) : BackgroundService
{
    private const string ProbeCommand =
        ". /etc/os-release 2>/dev/null; echo OS=$PRETTY_NAME; echo CPU=$(nproc); " +
        "echo RAM_MB=$(free -m | awk '/Mem:/{print $2}'); echo DISK=$(df -BG / | awk 'NR==2{print $2}'); " +
        "echo PORTS=$(ss -tlnH 2>/dev/null | awk '{print $4}' | sed 's/.*://' | sort -un | paste -sd, -)";

    private readonly SshDiscoveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Servers.Count == 0)
        {
            logger.LogInformation("SSH discovery idle — no servers configured.");
            return;
        }

        // Let the schema/DB settle before the first sweep.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        var interval = TimeSpan.FromMinutes(Math.Max(5, _options.IntervalMinutes));
        logger.LogInformation("SSH discovery started: {Count} server(s), every {Minutes}m.", _options.Servers.Count, interval.TotalMinutes);

        using var timer = new PeriodicTimer(interval);
        do
        {
            foreach (var target in _options.Servers)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;
                try
                {
                    await DiscoverAsync(target, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "SSH discovery failed for {Server}.", target.Name);
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DiscoverAsync(SshTarget target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(target.Name) || string.IsNullOrWhiteSpace(target.Host) || string.IsNullOrWhiteSpace(target.KeyPath))
            return;

        var server = await servers.FindByHostNameAsync(target.Name, cancellationToken);
        if (server is null)
            return;

        var output = await Task.Run(() => RunProbe(target), cancellationToken);
        if (output is null)
            return;

        var fields = Parse(output);
        server.ApplyDiscovery(
            fields.GetValueOrDefault("OS"),
            ParseInt(fields.GetValueOrDefault("CPU")),
            ParseRamGb(fields.GetValueOrDefault("RAM_MB")),
            ParseDiskGb(fields.GetValueOrDefault("DISK")),
            target.Host,
            fields.GetValueOrDefault("PORTS"),
            clock.GetUtcNow());

        await servers.UpdateAsync(server, cancellationToken);
        logger.LogInformation("SSH-discovered {Server} ({Os}).", target.Name, fields.GetValueOrDefault("OS"));
    }

    private static string? RunProbe(SshTarget target)
    {
        var keyPath = ExpandPath(target.KeyPath);
        if (!File.Exists(keyPath))
            return null;

        using var key = new PrivateKeyFile(keyPath);
        using var client = new SshClient(
            target.Host,
            target.Port <= 0 ? 22 : target.Port,
            string.IsNullOrWhiteSpace(target.User) ? "root" : target.User,
            key);
        client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(12);

        client.Connect();
        try
        {
            using var command = client.RunCommand(ProbeCommand);
            return command.Result;
        }
        finally
        {
            client.Disconnect();
        }
    }

    private static string ExpandPath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        if (expanded.StartsWith('~'))
            expanded = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + expanded[1..];
        return expanded;
    }

    private static Dictionary<string, string> Parse(string output)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in output.Split('\n'))
        {
            var index = line.IndexOf('=');
            if (index <= 0)
                continue;
            var key = line[..index].Trim();
            var value = line[(index + 1)..].Trim();
            if (key.Length > 0 && value.Length > 0)
                map[key] = value;
        }
        return map;
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double? ParseRamGb(string? megabytes)
        => double.TryParse(megabytes, NumberStyles.Any, CultureInfo.InvariantCulture, out var mb) ? Math.Round(mb / 1024.0, 1) : null;

    private static double? ParseDiskGb(string? disk)
    {
        if (string.IsNullOrEmpty(disk))
            return null;
        var digits = disk.TrimEnd('G', 'g', 'B', 'b');
        return double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }
}
