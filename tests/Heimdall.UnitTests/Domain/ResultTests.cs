using FluentAssertions;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.UnitTests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_carries_error()
    {
        var error = Error.Validation("X.Bad", "bad");
        var result = Result.Failure(error);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Generic_success_exposes_value()
    {
        Result<int> result = 42;
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Generic_failure_value_access_throws()
    {
        Result<int> result = Error.Failure("X", "y");
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Match_branches_on_outcome()
    {
        Result<int> ok = 7;
        Result<int> fail = Error.NotFound("X", "y");

        ok.Match(v => $"ok:{v}", e => $"err:{e.Code}").Should().Be("ok:7");
        fail.Match(v => $"ok:{v}", e => $"err:{e.Code}").Should().Be("err:X");
    }
}
