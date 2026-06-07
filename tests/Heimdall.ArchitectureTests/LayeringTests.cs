using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace Heimdall.ArchitectureTests;

public sealed class LayeringTests
{
    private static readonly Assembly DomainAssembly = typeof(Heimdall.Domain.SharedKernel.Result).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Heimdall.Application.Abstractions.IHostRepository).Assembly;

    [Fact]
    public void Domain_should_not_depend_on_outer_layers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Heimdall.Application", "Heimdall.Infrastructure", "Heimdall.Api", "Heimdall.Contracts")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Explain(result));
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Heimdall.Infrastructure", "Heimdall.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Explain(result));
    }

    [Fact]
    public void Handlers_should_reside_in_Application()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Handler")
            .Should().ResideInNamespaceStartingWith("Heimdall.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Explain(result));
    }

    private static string Explain(TestResult result)
        => "Offending types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
