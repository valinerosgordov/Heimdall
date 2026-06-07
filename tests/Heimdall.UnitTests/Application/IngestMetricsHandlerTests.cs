using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Metrics;
using Heimdall.Application.Security;
using Heimdall.Contracts;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.Metrics;
using Heimdall.Domain.SharedKernel;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class IngestMetricsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static MetricBatchRequest Batch(string host) => new()
    {
        HostName = host,
        Samples = [new MetricSampleDto { Metric = "cpu.usage", Value = 10, TimestampUnixMs = Now.ToUnixTimeMilliseconds() }],
    };

    [Fact]
    public async Task Unknown_host_is_unauthorized()
    {
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync("h", Arg.Any<CancellationToken>()).Returns((MonitoredHost?)null);
        var handler = new IngestMetricsHandler(hosts, Substitute.For<IMetricRepository>(), new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(Batch("h"), "key", default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Wrong_key_is_unauthorized_and_stores_nothing()
    {
        var host = MonitoredHost.Register("h", Now).Value;
        host.AssignAgentKey(KeyHasher.Hash("correct"));
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync(host.Name, Arg.Any<CancellationToken>()).Returns(host);
        var metrics = Substitute.For<IMetricRepository>();
        var handler = new IngestMetricsHandler(hosts, metrics, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(Batch(host.Name), "wrong", default);

        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        await metrics.DidNotReceive().InsertSamplesAsync(Arg.Any<IReadOnlyList<MetricSample>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Correct_key_touches_host_and_stores_samples()
    {
        var host = MonitoredHost.Register("h", Now).Value;
        host.AssignAgentKey(KeyHasher.Hash("correct"));
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync(host.Name, Arg.Any<CancellationToken>()).Returns(host);
        var metrics = Substitute.For<IMetricRepository>();
        var handler = new IngestMetricsHandler(hosts, metrics, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(Batch(host.Name), "correct", default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Accepted.Should().Be(1);
        result.Value.Rejected.Should().Be(0);
        await hosts.Received(1).TouchAsync(host.Id, Now, Arg.Any<CancellationToken>());
        await metrics.Received(1).InsertSamplesAsync(Arg.Is<IReadOnlyList<MetricSample>>(s => s.Count == 1), Arg.Any<CancellationToken>());
    }
}
