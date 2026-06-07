using FluentAssertions;
using Heimdall.Domain.Inventory;

namespace Heimdall.UnitTests.Domain;

public sealed class InventoryDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ServerDraft Draft(
        string? name = "web-01",
        decimal? cost = null,
        int? cpu = null,
        DateOnly? paidUntil = null)
        => new(name, "Timeweb", "10.0.0.1", "host", "Prod", cpu, 4, 50, "MSK", cost, "RUB", paidUntil, 12, "note", null, null);

    [Fact]
    public void Create_valid_server()
    {
        var result = Server.Create(Draft(), Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("web-01");
        result.Value.Provider.Should().Be("Timeweb");
        result.Value.UserCount.Should().Be(12);
        result.Value.CreatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_empty_name(string? name)
        => Server.Create(Draft(name: name), Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Create_rejects_negative_cost()
        => Server.Create(Draft(cost: -5m), Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Create_rejects_negative_cpu()
        => Server.Create(Draft(cpu: -1), Now).IsFailure.Should().BeTrue();

    [Fact]
    public void Create_encodes_html_in_name()
    {
        var result = Server.Create(Draft(name: "<script>x</script>"), Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().NotContain("<");
    }

    [Fact]
    public void Create_preserves_paid_until()
    {
        var due = new DateOnly(2026, 3, 15);
        Server.Create(Draft(paidUntil: due), Now).Value.PaidUntil.Should().Be(due);
    }

    [Fact]
    public void Update_changes_fields_and_stamps_updated()
    {
        var server = Server.Create(Draft(), Now).Value;
        var later = Now.AddDays(1);

        var updated = server.Update(Draft(name: "web-02"), later);

        updated.IsSuccess.Should().BeTrue();
        server.Name.Should().Be("web-02");
        server.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void ServerLink_rejects_self_link()
    {
        var id = Guid.NewGuid();
        ServerLink.Create(id, id, "proxy").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ServerLink_defaults_kind_when_missing()
    {
        var link = ServerLink.Create(Guid.NewGuid(), Guid.NewGuid(), null);

        link.IsSuccess.Should().BeTrue();
        link.Value.Kind.Should().Be("depends-on");
    }

    [Fact]
    public void ServerLink_valid()
    {
        var link = ServerLink.Create(Guid.NewGuid(), Guid.NewGuid(), "proxy");

        link.IsSuccess.Should().BeTrue();
        link.Value.Kind.Should().Be("proxy");
    }
}
