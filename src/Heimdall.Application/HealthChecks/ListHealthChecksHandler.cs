using Heimdall.Application.Abstractions;
using Heimdall.Contracts;

namespace Heimdall.Application.HealthChecks;

public sealed class ListHealthChecksHandler(IHealthCheckRepository repository)
{
    public async Task<IReadOnlyList<HealthCheckStatusDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var statuses = await repository.ListWithStatusAsync(cancellationToken);
        return
        [
            .. statuses.Select(s => new HealthCheckStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                Kind = s.Kind.ToString(),
                Target = s.Target,
                Enabled = s.Enabled,
                IsUp = s.IsUp,
                LatencyMs = s.LatencyMs,
                LastCheckedAtUnixMs = s.LastCheckedAt?.ToUnixTimeMilliseconds(),
                Uptime24h = s.Uptime24h,
            }),
        ];
    }
}
