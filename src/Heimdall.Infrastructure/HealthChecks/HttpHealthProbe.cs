using Heimdall.Application.HealthChecks;
using Heimdall.Domain.HealthChecks;

namespace Heimdall.Infrastructure.HealthChecks;

/// <summary>Probes an HTTP(S) endpoint. A 2xx–3xx response within the timeout counts as up.</summary>
internal sealed class HttpHealthProbe(IHttpClientFactory httpClientFactory, TimeProvider clock) : IHealthProbe
{
    public const string HttpClientName = "healthchecks";

    public ProbeKind Kind => ProbeKind.Http;

    public async Task<HealthProbeResult> ProbeAsync(HealthCheckTarget target, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        var start = clock.GetTimestamp();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, target.Target);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var elapsed = clock.GetElapsedTime(start).TotalMilliseconds;

            var statusCode = (int)response.StatusCode;
            var isUp = statusCode is >= 200 and < 400;
            return new HealthProbeResult(isUp, elapsed, isUp ? null : $"HTTP {statusCode}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new HealthProbeResult(false, clock.GetElapsedTime(start).TotalMilliseconds, ex.GetType().Name);
        }
    }
}
