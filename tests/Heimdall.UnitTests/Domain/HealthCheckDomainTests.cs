using FluentAssertions;
using Heimdall.Domain.HealthChecks;

namespace Heimdall.UnitTests.Domain;

public sealed class HealthCheckDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_valid_http_target()
    {
        var result = HealthCheckTarget.Create("api", "Http", "https://example.com/health", 30, Now);
        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(ProbeKind.Http);
    }

    [Fact]
    public void Create_valid_tcp_target()
    {
        var result = HealthCheckTarget.Create("db", "tcp", "localhost:5432", 30, Now);
        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(ProbeKind.Tcp);
    }

    [Theory]
    [InlineData("ping")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_rejects_invalid_kind(string? kind)
        => HealthCheckTarget.Create("x", kind, "https://example.com", 30, Now).IsFailure.Should().BeTrue();

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("")]
    public void Create_rejects_invalid_http_target(string target)
        => HealthCheckTarget.Create("x", "Http", target, 30, Now).IsFailure.Should().BeTrue();

    [Theory]
    [InlineData("hostonly")]
    [InlineData("host:notaport")]
    [InlineData("host:70000")]
    public void Create_rejects_invalid_tcp_target(string target)
        => HealthCheckTarget.Create("x", "Tcp", target, 30, Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Create_tls_target_defaults_to_443()
        => HealthCheckTarget.Create("cert", "Tls", "example.com", 60, Now).Value.Target.Should().Be("example.com:443");

    [Theory]
    [InlineData("example.com:8443", true)]
    [InlineData("example.com", true)]
    [InlineData("host:notaport", false)]
    [InlineData("", false)]
    public void Create_validates_tls_target(string target, bool expectSuccess)
        => HealthCheckTarget.Create("cert", "Tls", target, 60, Now).IsSuccess.Should().Be(expectSuccess);

    [Fact]
    public void Create_clamps_interval_to_minimum()
        => HealthCheckTarget.Create("x", "Http", "https://e.com", 1, Now).Value.IntervalSeconds.Should().Be(5);

    [Fact]
    public void IsDue_true_when_never_checked()
        => HealthCheckTarget.Create("x", "Http", "https://e.com", 30, Now).Value.IsDue(Now).Should().BeTrue();

    [Fact]
    public void IsDue_false_within_interval_after_check()
    {
        var target = HealthCheckTarget.Create("x", "Http", "https://e.com", 30, Now).Value;
        target.MarkChecked(Now);
        target.IsDue(Now.AddSeconds(10)).Should().BeFalse();
        target.IsDue(Now.AddSeconds(31)).Should().BeTrue();
    }

    [Theory]
    [InlineData("Http", true)]
    [InlineData("tcp", true)]
    [InlineData("ping", false)]
    public void ProbeKindParser_parses(string raw, bool expectSuccess)
        => ProbeKindParser.Parse(raw).IsSuccess.Should().Be(expectSuccess);
}
