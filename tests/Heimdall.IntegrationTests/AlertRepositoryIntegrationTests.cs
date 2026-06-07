using FluentAssertions;
using Heimdall.Domain.Alerting;
using Heimdall.Infrastructure.Persistence;

namespace Heimdall.IntegrationTests;

[Collection(nameof(TimescaleCollection))]
public sealed class AlertRepositoryIntegrationTests(TimescaleFixture fixture)
{
    [Fact]
    public async Task Rule_and_alert_lifecycle_roundtrips()
    {
        var repository = new AlertRepository(fixture.DataSource);
        var now = DateTimeOffset.UtcNow;
        var host = $"it-alert-host-{Guid.NewGuid():N}";

        var rule = AlertRule.Create($"it-rule-{Guid.NewGuid():N}", "cpu.usage", "gt", 90, 30, "critical", host, now).Value;
        await repository.AddRuleAsync(rule, CancellationToken.None);

        var enabled = await repository.GetEnabledRulesAsync(CancellationToken.None);
        enabled.Should().Contain(r => r.Id == rule.Id && r.Operator == ComparisonOperator.GreaterThan);

        var alert = Alert.Fire(rule, host, 95, now);
        await repository.InsertAlertAsync(alert, CancellationToken.None);

        var active = await repository.GetActiveAlertAsync(rule.Id, host, CancellationToken.None);
        active.Should().NotBeNull();
        active!.Value.Should().Be(95);
        active.Severity.Should().Be(AlertSeverity.Critical);
        active.Status.Should().Be(AlertStatus.Firing);

        await repository.ResolveAlertAsync(active.Id, now.AddMinutes(1), CancellationToken.None);
        (await repository.GetActiveAlertAsync(rule.Id, host, CancellationToken.None)).Should().BeNull();

        (await repository.DeleteRuleAsync(rule.Id, CancellationToken.None)).Should().BeTrue();
    }
}
