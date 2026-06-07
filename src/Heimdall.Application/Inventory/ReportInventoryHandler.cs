using Heimdall.Application.Abstractions;
using Heimdall.Application.Security;
using Heimdall.Contracts;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

/// <summary>Authenticates the agent by its key, then upserts the matching server's auto-discovered fields.</summary>
public sealed class ReportInventoryHandler(IHostRepository hosts, IServerRepository servers, TimeProvider clock)
{
    public async Task<Result> HandleAsync(InventoryReportRequest request, string? agentKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.HostName))
            return Result.Failure(Error.Validation("Host.NameEmpty", "Host name must not be empty."));

        var host = await hosts.GetByNameAsync(request.HostName, cancellationToken);
        if (host is null || !host.IsEnrolled)
            return Result.Failure(Error.Unauthorized("Ingest.NotEnrolled", "Host is not enrolled. Enroll the agent first."));
        if (string.IsNullOrEmpty(agentKey) || !KeyHasher.Verify(agentKey, host.AgentKeyHash!))
            return Result.Failure(Error.Unauthorized("Ingest.InvalidKey", "Invalid agent key."));

        var hostName = request.HostName.Trim();
        var now = clock.GetUtcNow();

        var existing = await servers.FindByHostNameAsync(hostName, cancellationToken);
        if (existing is not null)
        {
            existing.ApplyDiscovery(request.Os, request.CpuCores, request.RamGb, request.DiskGb, hostName, request.ListeningPorts, now);
            await servers.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var server = Server.CreateDiscovered(
                hostName, request.Os, request.CpuCores, request.RamGb, request.DiskGb, request.ListeningPorts, now);
            await servers.AddAsync(server, cancellationToken);
        }

        return Result.Success();
    }
}
