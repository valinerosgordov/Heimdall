using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Metrics;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.SharedKernel;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class QueryHostMetricsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] Cpu = ["cpu.usage"];

    private static (IHostRepository hosts, IMetricRepository metrics, MonitoredHost host) Arrange()
    {
        var host = MonitoredHost.Register("h", Now).Value;
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync(host.Name, Arg.Any<CancellationToken>()).Returns(host);
        var metrics = Substitute.For<IMetricRepository>();
        metrics.QueryAsync(Arg.Any<HostId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<MetricPoint>)[]);
        metrics.QueryBucketedAsync(Arg.Any<HostId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<MetricPoint>)[]);
        return (hosts, metrics, host);
    }

    [Fact]
    public async Task Unknown_host_returns_not_found()
    {
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync("ghost", Arg.Any<CancellationToken>()).Returns((MonitoredHost?)null);
        var handler = new QueryHostMetricsHandler(hosts, Substitute.For<IMetricRepository>(), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync("ghost", Cpu, 15, 500, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Narrow_window_uses_raw_query()
    {
        var (hosts, metrics, _) = Arrange();
        var handler = new QueryHostMetricsHandler(hosts, metrics, new FixedTimeProvider(Now));

        // 15 min / 500 points => ~1.8s bucket <= 5s cadence => raw.
        await handler.HandleAsync("h", Cpu, 15, 500, default);

        await metrics.Received(1).QueryAsync(Arg.Any<HostId>(), "cpu.usage", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        await metrics.DidNotReceive().QueryBucketedAsync(Arg.Any<HostId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Wide_window_downsamples()
    {
        var (hosts, metrics, _) = Arrange();
        var handler = new QueryHostMetricsHandler(hosts, metrics, new FixedTimeProvider(Now));

        // 24h / 500 points => ~173s bucket > 5s cadence => bucketed.
        await handler.HandleAsync("h", Cpu, 1440, 500, default);

        await metrics.Received(1).QueryBucketedAsync(Arg.Any<HostId>(), "cpu.usage", Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Is<int>(b => b > 5), Arg.Any<CancellationToken>());
        await metrics.DidNotReceive().QueryAsync(Arg.Any<HostId>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
