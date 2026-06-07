using FluentAssertions;
using Heimdall.Application.Abstractions;
using Heimdall.Application.Inventory;
using Heimdall.Contracts;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;
using Heimdall.UnitTests.TestSupport;
using NSubstitute;

namespace Heimdall.UnitTests.Application;

public sealed class ServerHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_valid_adds_server_and_computes_due()
    {
        var repo = Substitute.For<IServerRepository>();
        var handler = new CreateServerHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new CreateServerRequest { Name = "web-01", MonthlyCost = 600, Currency = "RUB", PaidUntil = "2026-01-20" },
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("web-01");
        result.Value.DaysUntilDue.Should().Be(10);
        await repo.Received(1).AddAsync(Arg.Any<Server>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_invalid_date_fails_without_adding()
    {
        var repo = Substitute.For<IServerRepository>();
        var handler = new CreateServerHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new CreateServerRequest { Name = "x", PaidUntil = "not-a-date" }, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        await repo.DidNotReceive().AddAsync(Arg.Any<Server>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_missing_returns_not_found()
    {
        var repo = Substitute.For<IServerRepository>();
        repo.GetAsync(Arg.Any<ServerId>(), Arg.Any<CancellationToken>()).Returns((Server?)null);
        var handler = new UpdateServerHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(Guid.NewGuid(), new CreateServerRequest { Name = "x" }, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task CreateLink_self_link_fails()
    {
        var repo = Substitute.For<IServerRepository>();
        var handler = new CreateServerLinkHandler(repo);
        var id = Guid.NewGuid();

        var result = await handler.HandleAsync(new CreateServerLinkRequest { FromServerId = id, ToServerId = id }, default);

        result.IsFailure.Should().BeTrue();
        await repo.DidNotReceive().AddLinkAsync(Arg.Any<ServerLink>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_computes_days_until_due_and_surfaces_status()
    {
        var repo = Substitute.For<IServerRepository>();
        var record = new ServerRecord(
            Guid.NewGuid(), "web", null, null, null, null, null, null, null, null,
            null, null, new DateOnly(2026, 1, 20), null, null, null, null, true);
        repo.ListWithStatusAsync(Arg.Any<CancellationToken>()).Returns([record]);
        repo.ListLinksAsync(Arg.Any<CancellationToken>()).Returns([]);
        var handler = new ListInventoryHandler(repo, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(default);

        result.Servers.Should().HaveCount(1);
        result.Servers[0].DaysUntilDue.Should().Be(10);
        result.Servers[0].IsUp.Should().BeTrue();
    }
}
