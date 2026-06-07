using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.HealthChecks;

namespace Heimdall.Application.HealthChecks;

public sealed class GetHealthCheckHistoryHandler(IHealthCheckRepository repository, TimeProvider clock)
{
    public async Task<HealthCheckHistoryResponse> HandleAsync(Guid id, int minutes, CancellationToken cancellationToken)
    {
        var to = clock.GetUtcNow();
        var from = to.AddMinutes(-Math.Clamp(minutes, 1, 1440));

        var rows = await repository.QueryResultsAsync(new HealthCheckTargetId(id), from, to, cancellationToken);

        return new HealthCheckHistoryResponse
        {
            TargetId = id,
            Points =
            [
                .. rows.Select(r => new HealthCheckResultPointDto
                {
                    TimestampUnixMs = r.TimestampUnixMs,
                    IsUp = r.IsUp,
                    LatencyMs = r.LatencyMs,
                }),
            ],
        };
    }
}
