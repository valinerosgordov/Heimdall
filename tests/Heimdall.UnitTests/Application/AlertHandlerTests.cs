using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Alerting;
using Heimdall.Contracts;
using Heimdall.Domain.Alerting;
using Heimdall.Domain.SharedKernel;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class AlertHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_valid_rule_persists_and_maps_dto()
    {
        var repo = Substitute.For<IAlertRepository>();
        var handler = new CreateAlertRuleHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new CreateAlertRuleRequest
        {
            Name = "cpu", Metric = "cpu.usage", Operator = "gt", Threshold = 90, DurationSeconds = 60, Severity = "critical",
        }, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Operator.Should().Be("gt");
        result.Value.Severity.Should().Be("critical");
        await repo.Received(1).AddRuleAsync(Arg.Any<AlertRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_invalid_rule_does_not_persist()
    {
        var repo = Substitute.For<IAlertRepository>();
        var handler = new CreateAlertRuleHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new CreateAlertRuleRequest
        {
            Name = "cpu", Metric = "cpu.usage", Operator = "between", Threshold = 90,
        }, default);

        result.IsFailure.Should().BeTrue();
        await repo.DidNotReceive().AddRuleAsync(Arg.Any<AlertRule>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_existing_rule_succeeds()
    {
        var repo = Substitute.For<IAlertRepository>();
        repo.DeleteRuleAsync(Arg.Any<AlertRuleId>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await new DeleteAlertRuleHandler(repo).HandleAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_missing_rule_is_not_found()
    {
        var repo = Substitute.For<IAlertRepository>();
        repo.DeleteRuleAsync(Arg.Any<AlertRuleId>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await new DeleteAlertRuleHandler(repo).HandleAsync(Guid.NewGuid(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
