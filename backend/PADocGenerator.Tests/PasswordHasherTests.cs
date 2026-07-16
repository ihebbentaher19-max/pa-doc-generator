using FluentAssertions;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ProducesSaltAndHashSeparatedByDot()
    {
        var hash = PasswordHasher.Hash("MotDePasse#2026");
        hash.Should().Contain(".");
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("MotDePasse#2026");
        PasswordHasher.Verify("MotDePasse#2026", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("MotDePasse#2026");
        PasswordHasher.Verify("MauvaisMotDePasse", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        var hash1 = PasswordHasher.Hash("MotDePasse#2026");
        var hash2 = PasswordHasher.Hash("MotDePasse#2026");
        hash1.Should().NotBe(hash2);
    }

    [Theory]
    [InlineData("hash-sans-separateur")]
    [InlineData("!!!invalide!!!.!!!invalide!!!")]
    public void Verify_MalformedHash_ReturnsFalseWithoutThrowing(string malformedHash)
    {
        var act = () => PasswordHasher.Verify("peu importe", malformedHash);
        act.Should().NotThrow();
        PasswordHasher.Verify("peu importe", malformedHash).Should().BeFalse();
    }
}
