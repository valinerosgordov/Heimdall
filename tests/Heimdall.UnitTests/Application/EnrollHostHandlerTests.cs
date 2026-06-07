using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Hosts;
using Heimdall.Contracts;
using Heimdall.Domain.Hosts;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class EnrollHostHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enroll_new_host_issues_key_and_persists_enrolled_host()
    {
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync("new-host", Arg.Any<CancellationToken>()).Returns((MonitoredHost?)null);
        var handler = new EnrollHostHandler(hosts, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new EnrollRequest { HostName = "new-host" }, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.HostName.Should().Be("new-host");
        result.Value.AgentKey.Should().NotBeNullOrWhiteSpace();
        await hosts.Received(1).EnrollAsync(Arg.Is<MonitoredHost>(h => h.IsEnrolled), Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Enroll_empty_name_fails()
    {
        var hosts = Substitute.For<IHostRepository>();
        hosts.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((MonitoredHost?)null);
        var handler = new EnrollHostHandler(hosts, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new EnrollRequest { HostName = "  " }, default);

        result.IsFailure.Should().BeTrue();
        await hosts.DidNotReceive().EnrollAsync(Arg.Any<MonitoredHost>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
