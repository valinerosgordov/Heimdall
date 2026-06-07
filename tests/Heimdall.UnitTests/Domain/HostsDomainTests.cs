using FluentAssertions;
using Heimdall.Domain.Hosts;

namespace Heimdall.UnitTests.Domain;

public sealed class HostsDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_valid_creates_unenrolled_host()
    {
        var result = MonitoredHost.Register("web-01", Now);

        result.IsSuccess.Should().BeTrue();
        var host = result.Value;
        host.Name.Should().Be("web-01");
        host.CreatedAt.Should().Be(Now);
        host.LastSeenAt.Should().Be(Now);
        host.IsEnrolled.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Register_rejects_empty_name(string? name)
        => MonitoredHost.Register(name, Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Register_rejects_too_long_name()
        => MonitoredHost.Register(new string('a', 201), Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Register_html_encodes_name()
        => MonitoredHost.Register("<script>", Now).Value.Name.Should().NotContain("<");

    [Fact]
    public void AssignAgentKey_marks_enrolled()
    {
        var host = MonitoredHost.Register("h", Now).Value;
        host.AssignAgentKey("abc123");
        host.IsEnrolled.Should().BeTrue();
        host.AgentKeyHash.Should().Be("abc123");
    }

    [Fact]
    public void Touch_updates_last_seen()
    {
        var host = MonitoredHost.Register("h", Now).Value;
        var later = Now.AddMinutes(5);
        host.Touch(later);
        host.LastSeenAt.Should().Be(later);
        host.CreatedAt.Should().Be(Now);
    }
}
