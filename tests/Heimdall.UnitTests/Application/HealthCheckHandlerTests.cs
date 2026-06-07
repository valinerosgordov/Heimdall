using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.HealthChecks;
using Heimdall.Contracts;
using Heimdall.Domain.HealthChecks;
using Heimdall.Domain.SharedKernel;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class HealthCheckHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_valid_adds_target()
    {
        var repo = Substitute.For<IHealthCheckRepository>();
        var handler = new CreateHealthCheckTargetHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new CreateHealthCheckRequest { Name = "api", Kind = "Http", Target = "https://example.com", IntervalSeconds = 30 },
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("api");
        result.Value.IsUp.Should().BeNull();
        await repo.Received(1).AddAsync(Arg.Any<HealthCheckTarget>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_invalid_does_not_add()
    {
        var repo = Substitute.For<IHealthCheckRepository>();
        var handler = new CreateHealthCheckTargetHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new CreateHealthCheckRequest { Name = "api", Kind = "Ping", Target = "x", IntervalSeconds = 30 },
            default);

        result.IsFailure.Should().BeTrue();
        await repo.DidNotReceive().AddAsync(Arg.Any<HealthCheckTarget>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_existing_succeeds()
    {
        var repo = Substitute.For<IHealthCheckRepository>();
        repo.DeleteAsync(Arg.Any<HealthCheckTargetId>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteHealthCheckTargetHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_missing_returns_not_found()
    {
        var repo = Substitute.For<IHealthCheckRepository>();
        repo.DeleteAsync(Arg.Any<HealthCheckTargetId>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteHealthCheckTargetHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}
