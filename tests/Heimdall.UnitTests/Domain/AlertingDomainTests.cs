using FluentAssertions;
using Heimdall.Domain.Alerting;

namespace Heimdall.UnitTests.Domain;

public sealed class AlertingDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("gt", true)]
    [InlineData(">", true)]
    [InlineData("lte", true)]
    [InlineData("between", false)]
    [InlineData("", false)]
    public void OperatorParser_validates(string raw, bool ok)
        => ComparisonOperatorParser.Parse(raw).IsSuccess.Should().Be(ok);

    [Theory]
    [InlineData("warning", true)]
    [InlineData("Critical", true)]
    [InlineData("fatal", false)]
    public void SeverityParser_validates(string raw, bool ok)
        => AlertSeverityParser.Parse(raw).IsSuccess.Should().Be(ok);

    [Fact]
    public void Create_builds_enabled_rule()
    {
        var rule = AlertRule.Create("cpu high", "cpu.usage", "gt", 90, 60, "critical", null, Now);
        rule.IsSuccess.Should().BeTrue();
        rule.Value.Operator.Should().Be(ComparisonOperator.GreaterThan);
        rule.Value.Severity.Should().Be(AlertSeverity.Critical);
        rule.Value.HostName.Should().BeNull();
        rule.Value.Enabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "cpu.usage", "gt", "critical")]
    [InlineData("n", "bad metric!", "gt", "critical")]
    [InlineData("n", "cpu.usage", "between", "critical")]
    [InlineData("n", "cpu.usage", "gt", "fatal")]
    public void Create_rejects_invalid(string? name, string metric, string op, string severity)
        => AlertRule.Create(name, metric, op, 1, 60, severity, null, Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Create_clamps_duration_to_minimum()
        => AlertRule.Create("n", "cpu.usage", "gt", 1, 1, "warning", null, Now).Value.DurationSeconds.Should().Be(10);

    [Theory]
    [InlineData("gt", 50, 60, true)]
    [InlineData("gt", 50, 40, false)]
    [InlineData("lt", 50, 40, true)]
    [InlineData("gte", 50, 50, true)]
    [InlineData("lte", 50, 50, true)]
    [InlineData("lte", 50, 60, false)]
    public void IsBreached_applies_operator(string op, double threshold, double value, bool expected)
        => AlertRule.Create("n", "cpu.usage", op, threshold, 60, "warning", null, Now).Value.IsBreached(value).Should().Be(expected);

    [Fact]
    public void Alert_fires_then_resolves()
    {
        var rule = AlertRule.Create("n", "cpu.usage", "gt", 50, 60, "critical", "h", Now).Value;
        var alert = Alert.Fire(rule, "h", 80, Now);

        alert.Status.Should().Be(AlertStatus.Firing);
        alert.Value.Should().Be(80);

        alert.Resolve(Now.AddMinutes(1));
        alert.Status.Should().Be(AlertStatus.Resolved);
        alert.ResolvedAt.Should().Be(Now.AddMinutes(1));
    }
}
