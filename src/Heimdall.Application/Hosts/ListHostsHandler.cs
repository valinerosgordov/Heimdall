using Heimdall.Application.Abstractions;
using Heimdall.Contracts;

namespace Heimdall.Application.Hosts;

/// <summary>Lists all monitored hosts for the overview grid.</summary>
public sealed class ListHostsHandler(IHostRepository hosts)
{
    public async Task<IReadOnlyList<HostSummaryDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var summaries = await hosts.ListAsync(cancellationToken);
        return
        [
            .. summaries.Select(s => new HostSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                CreatedAtUnixMs = s.CreatedAt.ToUnixTimeMilliseconds(),
                LastSeenAtUnixMs = s.LastSeenAt.ToUnixTimeMilliseconds(),
            }),
        ];
    }
}
