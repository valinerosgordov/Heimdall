using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Domain.HealthChecks;
using Heimdall.Domain.Hosts;
using Heimdall.Domain.Inventory;
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

[Collection(nameof(TimescaleCollection))]
public sealed class ServerRepositoryIntegrationTests(TimescaleFixture fixture)
{
    private static ServerDraft MinimalDraft(string name)
        => new(name, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

    [Fact]
    public async Task Add_with_paid_until_then_list_and_get_roundtrips()
    {
        var repository = new ServerRepository(fixture.DataSource);
        var now = DateTimeOffset.UtcNow;
        var due = DateOnly.FromDateTime(now.UtcDateTime).AddDays(14);
        var draft = new ServerDraft(
            $"it-srv-{Guid.NewGuid():N}", "Timeweb", "10.0.0.9", "host", "Prod",
            2, 4, 50, "MSK", 600m, "RUB", due, 17, "note", null, null);
        var server = Server.Create(draft, now).Value;

        await repository.AddAsync(server, CancellationToken.None);

        var loaded = (await repository.ListWithStatusAsync(CancellationToken.None)).Single(s => s.Id == server.Id.Value);
        loaded.PaidUntil.Should().Be(due);
        loaded.MonthlyCost.Should().Be(600m);
        loaded.UserCount.Should().Be(17);

        var fetched = await repository.GetAsync(server.Id, CancellationToken.None);
        fetched.Should().NotBeNull();
        fetched!.PaidUntil.Should().Be(due);

        (await repository.DeleteAsync(server.Id, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Link_add_list_then_delete()
    {
        var repository = new ServerRepository(fixture.DataSource);
        var now = DateTimeOffset.UtcNow;
        var a = Server.Create(MinimalDraft($"it-a-{Guid.NewGuid():N}"), now).Value;
        var b = Server.Create(MinimalDraft($"it-b-{Guid.NewGuid():N}"), now).Value;
        await repository.AddAsync(a, CancellationToken.None);
        await repository.AddAsync(b, CancellationToken.None);

        var link = ServerLink.Create(a.Id.Value, b.Id.Value, "proxy").Value;
        await repository.AddLinkAsync(link, CancellationToken.None);

        var links = await repository.ListLinksAsync(CancellationToken.None);
        links.Should().Contain(l => l.Id == link.Id && l.Kind == "proxy");

        (await repository.DeleteLinkAsync(link.Id, CancellationToken.None)).Should().BeTrue();
    }
}

[Collection(nameof(TimescaleCollection))]
public sealed class OperatorStoreIntegrationTests(TimescaleFixture fixture)
{
    [Fact]
    public async Task Set_then_Get_roundtrips_and_reports_configured()
    {
        var store = new OperatorStore(fixture.DataSource);

        await store.SetAsync("opuser", "hashvalue", CancellationToken.None);

        var creds = await store.GetAsync(CancellationToken.None);
        creds.Should().NotBeNull();
        creds!.Value.Username.Should().Be("opuser");
        creds.Value.PasswordSha256.Should().Be("hashvalue");
        (await store.IsConfiguredAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Set_is_idempotent_upsert()
    {
        var store = new OperatorStore(fixture.DataSource);

        await store.SetAsync("first", "h1", CancellationToken.None);
        await store.SetAsync("second", "h2", CancellationToken.None);

        var creds = await store.GetAsync(CancellationToken.None);
        creds!.Value.Username.Should().Be("second");
        creds.Value.PasswordSha256.Should().Be("h2");
    }
}
