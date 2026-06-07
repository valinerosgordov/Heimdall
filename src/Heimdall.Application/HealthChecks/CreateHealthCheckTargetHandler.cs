using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.HealthChecks;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.HealthChecks;

public sealed class CreateHealthCheckTargetHandler(IHealthCheckRepository repository, TimeProvider clock)
{
    public async Task<Result<HealthCheckStatusDto>> HandleAsync(CreateHealthCheckRequest request, CancellationToken cancellationToken)
    {
        var target = HealthCheckTarget.Create(request.Name, request.Kind, request.Target, request.IntervalSeconds, clock.GetUtcNow());
        if (target.IsFailure)
            return target.Error;

        await repository.AddAsync(target.Value, cancellationToken);

        var created = target.Value;
        return new HealthCheckStatusDto
        {
            Id = created.Id.Value,
            Name = created.Name,
            Kind = created.Kind.ToString(),
            Target = created.Target,
            Enabled = created.Enabled,
            IsUp = null,
            LatencyMs = null,
            LastCheckedAtUnixMs = null,
        };
    }
}
