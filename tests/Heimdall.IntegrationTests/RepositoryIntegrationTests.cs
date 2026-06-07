using FluentAssertions;
using Heimdall.Domain.HealthChecks;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.Metrics;
using Heimdall.Infrastructure.Persistence;

namespace Heimdall.IntegrationTests;

[Collection(nameof(TimescaleCollection))]
public sealed class HostRepositoryIntegrationTests(TimescaleFixture fixture)
{
    [Fact]
    public async Task Enroll_then_GetByName_returns_enrolled_host()
    {
        var repository = new HostRepository(fixture.DataSource);
        var now = DateTimeOffset.UtcNow;
        var host = MonitoredHost.Register($"it-host-{Guid.NewGuid():N}", now).Value;
        host.AssignAgentKey("deadbeefhash");

        await repository.EnrollAsync(host, now, CancellationToken.None);
        var loaded = await repository.GetByNameAsync(host.Name, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.IsEnrolled.Should().BeTrue();
        loaded.AgentKeyHash.Should().Be("deadbeefhash");
    }
}

[Collection(nameof(TimescaleCollection))]
public sealed class MetricRepositoryIntegrationTests(TimescaleFixture fixture)
{
    [Fact]
    public async Task InsertSamples_then_Query_roundtrips()
    {
        var hosts = new HostRepository(fixture.DataSource);
        var metrics = new MetricRepository(fixture.DataSource);
        var now = DateTimeOffset.UtcNow;

        var host = MonitoredHost.Register($"it-metrics-{Guid.NewGuid():N}", now).Value;
        host.AssignAgentKey("k");
        await hosts.EnrollAsync(host, now, CancellationToken.None);

        var sample = MetricSample.Create(host.Id, MetricName.Create("cpu.usage").Value, 42.5, now).Value;
        await metrics.InsertSamplesAsync([sample], CancellationToken.None);

        var points = await metrics.QueryAsync(host.Id, "cpu.usage", now.AddMinutes(-5), now.AddMinutes(5), CancellationToken.None);

        points.Should().ContainSingle();
        points[0].Value.Should().Be(42.5);
    }
}

[Collection(nameof(TimescaleCollection))]
public sealed class HealthCheckRepositoryIntegrationTests(TimescaleFixture fixture)
{
    [Fact]
    public async Task Add_record_list_then_delete()
    {
        var repository = new HealthCheckRepository(fixture.DataSource);
        var now = DateTimeOffset.UtcNow;
        var target = HealthCheckTarget.Create($"it-hc-{Guid.NewGuid():N}", "Http", "http://localhost/health", 10, now).Value;

        await repository.AddAsync(target, CancellationToken.None);
        await repository.RecordResultAsync(target.Id, isUp: true, latencyMs: 12.5, now, CancellationToken.None);

        var statuses = await repository.ListWithStatusAsync(CancellationToken.None);
        var status = statuses.Single(s => s.Id == target.Id.Value);
        status.IsUp.Should().BeTrue();
        status.LatencyMs.Should().Be(12.5);

        (await repository.DeleteAsync(target.Id, CancellationToken.None)).Should().BeTrue();
    }
}
