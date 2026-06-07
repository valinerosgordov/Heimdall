using Heimdall.Application.Abstractions;
using Heimdall.Contracts;

namespace Heimdall.Application.Alerting;

public sealed class ListAlertsHandler(IAlertRepository repository)
{
    public async Task<IReadOnlyList<AlertDto>> HandleAsync(int limit, CancellationToken cancellationToken)
    {
        var alerts = await repository.ListAlertsAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        return [.. alerts.Select(a => a.ToDto())];
    }
}
