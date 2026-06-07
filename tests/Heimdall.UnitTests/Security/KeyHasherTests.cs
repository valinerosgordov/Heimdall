using FluentAssertions;
using Heimdall.Application.Security;

namespace Heimdall.UnitTests.Security;

public sealed class KeyHasherTests
{
    [Fact]
    public void Hash_is_deterministic_64_char_hex()
    {
        var a = KeyHasher.Hash("my-key");
        var b = KeyHasher.Hash("my-key");
        a.Should().Be(b);
        a.Should().HaveLength(64);
        a.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Verify_true_for_matching_key()
    {
        var hash = KeyHasher.Hash("secret");
        KeyHasher.Verify("secret", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_false_for_wrong_key()
    {
        var hash = KeyHasher.Hash("secret");
        KeyHasher.Verify("not-secret", hash).Should().BeFalse();
    }
}
