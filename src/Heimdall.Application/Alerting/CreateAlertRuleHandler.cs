using Heimdall.Application.Abstractions;
using Heimdall.Contracts;
using Heimdall.Domain.Alerting;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Alerting;

public sealed class CreateAlertRuleHandler(IAlertRepository repository, TimeProvider clock)
{
    public async Task<Result<AlertRuleDto>> HandleAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = AlertRule.Create(
            request.Name, request.Metric, request.Operator, request.Threshold,
            request.DurationSeconds, request.Severity, request.HostName, clock.GetUtcNow());

        if (rule.IsFailure)
            return rule.Error;

        await repository.AddRuleAsync(rule.Value, cancellationToken);
        return rule.Value.ToDto();
    }
}
