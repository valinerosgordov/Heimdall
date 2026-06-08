using FluentAssertions;
using Heimdall.Application.Security;

namespace Heimdall.UnitTests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_then_Verify_roundtrips()
    {
        var encoded = PasswordHasher.Hash("correct horse battery staple");

        PasswordHasher.Verify("correct horse battery staple", encoded).Should().BeTrue();
        PasswordHasher.Verify("wrong password", encoded).Should().BeFalse();
    }

    [Fact]
    public void Hash_is_salted_so_same_password_differs()
        => PasswordHasher.Hash("same").Should().NotBe(PasswordHasher.Hash("same"));

    [Fact]
    public void Encoded_format_is_pbkdf2_sha256()
        => PasswordHasher.Hash("x").Should().StartWith("pbkdf2$sha256$");

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2$sha256$notanumber$salt$hash")]
    [InlineData("pbkdf2$md5$600000$c2FsdA==$aGFzaA==")]
    public void Verify_rejects_malformed(string encoded)
        => PasswordHasher.Verify("x", encoded).Should().BeFalse();
}
