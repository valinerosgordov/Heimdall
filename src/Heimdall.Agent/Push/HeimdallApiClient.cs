using System.Net;
using System.Net.Http.Json;
using Heimdall.Contracts;
using Microsoft.Extensions.Logging;

namespace Heimdall.Agent.Push;

public enum PushOutcome
{
    Success,
    Unauthorized,
    Failed,
}

/// <summary>Talks to the Heimdall API: one-time enrollment and per-tick metric pushes.</summary>
internal sealed class HeimdallApiClient(HttpClient httpClient, ILogger<HeimdallApiClient> logger)
{
    public async Task<string?> EnrollAsync(string hostName, string enrollmentKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/enroll")
            {
                Content = JsonContent.Create(
                    new EnrollRequest { HostName = hostName }, HeimdallJsonContext.Default.EnrollRequest),
            };
            request.Headers.Add("X-Heimdall-Enrollment-Key", enrollmentKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Enrollment failed with status {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync(HeimdallJsonContext.Default.EnrollResponse, cancellationToken);
            return result?.AgentKey;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Enrollment request to {BaseAddress} failed.", httpClient.BaseAddress);
            return null;
        }
    }

    public async Task<PushOutcome> PushAsync(MetricBatchRequest batch, string agentKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/ingest/metrics")
            {
                Content = JsonContent.Create(batch, HeimdallJsonContext.Default.MetricBatchRequest),
            };
            request.Headers.Add("X-Heimdall-Key", agentKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return PushOutcome.Success;

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return PushOutcome.Unauthorized;

            logger.LogWarning("Ingest returned status {StatusCode}.", (int)response.StatusCode);
            return PushOutcome.Failed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to push metrics to {BaseAddress}.", httpClient.BaseAddress);
            return PushOutcome.Failed;
        }
    }

    /// <summary>Reports a host inventory snapshot (auto-discovery). Returns true on success.</summary>
    public async Task<bool> ReportInventoryAsync(InventoryReportRequest report, string agentKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/ingest/inventory")
            {
                Content = JsonContent.Create(report, HeimdallJsonContext.Default.InventoryReportRequest),
            };
            request.Headers.Add("X-Heimdall-Key", agentKey);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Inventory report returned status {StatusCode}.", (int)response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to report inventory to {BaseAddress}.", httpClient.BaseAddress);
            return false;
        }
    }
}
