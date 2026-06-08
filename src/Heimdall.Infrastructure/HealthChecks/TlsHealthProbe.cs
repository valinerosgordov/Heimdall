using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Heimdall.Application.HealthChecks;
using Heimdall.Domain.HealthChecks;

namespace Heimdall.Infrastructure.HealthChecks;

/// <summary>
/// Probes a TLS endpoint ("host" or "host:port"). Up while the served certificate is unexpired;
/// the result's latency value carries days-to-expiry so the board can warn before a cert lapses.
/// The chain is intentionally not validated — we only need to read the certificate's expiry.
/// </summary>
internal sealed class TlsHealthProbe(TimeProvider clock) : IHealthProbe
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public ProbeKind Kind => ProbeKind.Tls;

    public async Task<HealthProbeResult> ProbeAsync(HealthCheckTarget target, CancellationToken cancellationToken)
    {
        var parts = target.Target.Split(':', 2);
        var host = parts[0];
        var port = parts.Length == 2 && int.TryParse(parts[1], out var parsed) ? parsed : 443;

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Timeout);

            await client.ConnectAsync(host, port, timeoutCts.Token);

            await using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, timeoutCts.Token);

            if (ssl.RemoteCertificate is null)
                return new HealthProbeResult(false, 0, "No certificate presented");

            using var certificate = new X509Certificate2(ssl.RemoteCertificate);
            var daysToExpiry = Math.Floor((certificate.NotAfter.ToUniversalTime() - clock.GetUtcNow().UtcDateTime).TotalDays);

            return new HealthProbeResult(daysToExpiry >= 0, daysToExpiry, daysToExpiry >= 0 ? null : "Certificate expired");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeout or handshake failure → treat as down.
            return new HealthProbeResult(false, 0, ex.GetType().Name);
        }
    }
}
