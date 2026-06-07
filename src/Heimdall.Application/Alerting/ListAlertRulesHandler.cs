using Heimdall.Application.Abstractions;
using Heimdall.Contracts;

namespace Heimdall.Application.Alerting;

public sealed class ListAlertRulesHandler(IAlertRepository repository)
{
    public async Task<IReadOnlyList<AlertRuleDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var rules = await repository.ListRulesAsync(cancellationToken);
        return [.. rules.Select(r => r.ToDto())];
    }
}
