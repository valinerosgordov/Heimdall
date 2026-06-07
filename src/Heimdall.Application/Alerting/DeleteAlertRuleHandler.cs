using Heimdall.Application.Abstractions;
using Heimdall.Domain.Alerting;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Alerting;

public sealed class DeleteAlertRuleHandler(IAlertRepository repository)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteRuleAsync(new AlertRuleId(id), cancellationToken);
        return deleted
            ? Result.Success()
            : Result.Failure(Error.NotFound("Alert.RuleNotFound", $"Alert rule '{id}' was not found."));
    }
}
