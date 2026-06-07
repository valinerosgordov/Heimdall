using System.Security.Cryptography;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Security;
using Heimdall.Contracts;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Hosts;

/// <summary>Registers (or rotates the key of) a host and issues a fresh agent key. The key is returned once.</summary>
public sealed class EnrollHostHandler(IHostRepository hosts, TimeProvider clock)
{
    public async Task<Result<EnrollResponse>> HandleAsync(EnrollRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var host = await hosts.GetByNameAsync(request.HostName, cancellationToken);
        if (host is null)
        {
            var registration = MonitoredHost.Register(request.HostName, now);
            if (registration.IsFailure)
                return registration.Error;
            host = registration.Value;
        }

        var agentKey = GenerateKey();
        host.AssignAgentKey(KeyHasher.Hash(agentKey));
        await hosts.EnrollAsync(host, now, cancellationToken);

        return new EnrollResponse { HostName = host.Name, AgentKey = agentKey };
    }

    private static string GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
