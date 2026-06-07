using System.Net.Sockets;
using Heimdall.Application.HealthChecks;
using Heimdall.Domain.HealthChecks;

namespace Heimdall.Infrastructure.HealthChecks;

/// <summary>Probes a TCP endpoint ("host:port"). A successful connect within the timeout counts as up.</summary>
internal sealed class TcpHealthProbe(TimeProvider clock) : IHealthProbe
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    public ProbeKind Kind => ProbeKind.Tcp;

    public async Task<HealthProbeResult> ProbeAsync(HealthCheckTarget target, CancellationToken cancellationToken)
    {
        var parts = target.Target.Split(':', 2);
        var host = parts[0];
        var port = int.Parse(parts[1]);

        var start = clock.GetTimestamp();
        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ConnectTimeout);

            await client.ConnectAsync(host, port, timeoutCts.Token);
            var elapsed = clock.GetElapsedTime(start).TotalMilliseconds;
            return new HealthProbeResult(client.Connected, elapsed, client.Connected ? null : "Not connected");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeout (linked-token cancellation) or socket failure → target is down.
            return new HealthProbeResult(false, clock.GetElapsedTime(start).TotalMilliseconds, ex.GetType().Name);
        }
    }
}
