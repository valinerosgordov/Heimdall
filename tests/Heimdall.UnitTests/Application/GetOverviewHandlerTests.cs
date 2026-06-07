using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.HealthChecks;
using Heimdall.Application.Hosts;
using Heimdall.Application.Metrics;
using Heimdall.Application.Overview;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class GetOverviewHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Builds_snapshot_with_online_state_and_latest_values()
    {
        var onlineId = Guid.NewGuid();
        var offlineId = Guid.NewGuid();

        var hosts = Substitute.For<IHostRepository>();
        hosts.ListAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<HostSummary>)
        [
            new HostSummary(onlineId, "live", Now.AddDays(-1), Now.AddSeconds(-5)),
            new HostSummary(offlineId, "stale", Now.AddDays(-1), Now.AddMinutes(-10)),
        ]);

        var metrics = Substitute.For<IMetricRepository>();
        metrics.GetLatestPerHostAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<LatestMetric>)
        [
            new LatestMetric(onlineId, "cpu.usage", 42),
            new LatestMetric(onlineId, "memory.usage", 71),
        ]);

        var healthChecks = Substitute.For<IHealthCheckRepository>();
        healthChecks.ListWithStatusAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<HealthCheckStatus>)[]);

        var handler = new GetOverviewHandler(hosts, metrics, healthChecks, new FixedTimeProvider(Now));

        var snapshot = await handler.HandleAsync(default);

        var live = snapshot.Hosts.Single(h => h.Name == "live");
        live.Online.Should().BeTrue();
        live.Cpu.Should().Be(42);
        live.Memory.Should().Be(71);
        live.Disk.Should().BeNull();

        var stale = snapshot.Hosts.Single(h => h.Name == "stale");
        stale.Online.Should().BeFalse();
        stale.Cpu.Should().BeNull();
    }
}
