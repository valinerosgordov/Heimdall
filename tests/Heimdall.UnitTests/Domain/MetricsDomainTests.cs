using FluentAssertions;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.Metrics;

namespace Heimdall.UnitTests.Domain;

public sealed class MetricsDomainTests
{
    [Theory]
    [InlineData("cpu.usage")]
    [InlineData("net.rx_bytes_per_sec")]
    [InlineData("disk.usage")]
    public void MetricName_accepts_valid_names(string name)
        => MetricName.Create(name).IsSuccess.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("cpu usage")]
    [InlineData("cpu/usage")]
    [InlineData("drop;table")]
    public void MetricName_rejects_invalid_names(string name)
        => MetricName.Create(name).IsFailure.Should().BeTrue();

    [Fact]
    public void MetricName_rejects_too_long()
        => MetricName.Create(new string('a', 101)).IsFailure.Should().BeTrue();

    [Fact]
    public void MetricSample_accepts_finite_value()
    {
        var name = MetricName.Create("cpu.usage").Value;
        MetricSample.Create(HostId.New(), name, 42.5, DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void MetricSample_rejects_non_finite_value(double value)
    {
        var name = MetricName.Create("cpu.usage").Value;
        MetricSample.Create(HostId.New(), name, value, DateTimeOffset.UtcNow).IsFailure.Should().BeTrue();
    }
}
