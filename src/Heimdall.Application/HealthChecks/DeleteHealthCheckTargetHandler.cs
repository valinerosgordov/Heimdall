using Heimdall.Application.Abstractions;
using Heimdall.Domain.HealthChecks;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.HealthChecks;

public sealed class DeleteHealthCheckTargetHandler(IHealthCheckRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(new HealthCheckTargetId(id), cancellationToken);
        return deleted
            ? Result.Success()
            : Result.Failure(Error.NotFound("HealthCheck.NotFound", $"Health check '{id}' was not found."));
    }
}
