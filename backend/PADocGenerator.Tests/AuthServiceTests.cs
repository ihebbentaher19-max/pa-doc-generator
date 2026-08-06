using FluentAssertions;
using Microsoft.Extensions.Options;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services;
using Xunit;

namespace PADocGenerator.Tests;

public class AuthServiceTests
{
    private static AuthService CreateSut(out Api.Data.AppDbContext db)
    {
        db = TestDbContextFactory.Create();
        var jwtOptions = Options.Create(new JwtOptions
        {
            SigningKey = "test-signing-key-at-least-32-characters-long",
            Issuer = "PADocGeneratorTests",
            Audience = "PADocGeneratorTests",
            ExpirationMinutes = 60
        });
        return new AuthService(db, jwtOptions);
    }

    [Fact]
    public async Task RegisterAsync_FirstUser_BecomesAdministrateur()
    {
        var sut = CreateSut(out _);

        var result = await sut.RegisterAsync(new RegisterUserDto("Jane Doe", "jane@contoso.com", "MotDePasse#2026"));

        result.Role.Should().Be("Administrateur");
        result.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterAsync_SecondUser_BecomesUtilisateur()
    {
        var sut = CreateSut(out _);
        await sut.RegisterAsync(new RegisterUserDto("Admin", "admin@contoso.com", "MotDePasse#2026"));

        var second = await sut.RegisterAsync(new RegisterUserDto("Bob", "bob@contoso.com", "AutreMotDePasse#1"));

        second.Role.Should().Be("Utilisateur");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out _);
        await sut.RegisterAsync(new RegisterUserDto("Jane Doe", "jane@contoso.com", "MotDePasse#2026"));

        var act = () => sut.RegisterAsync(new RegisterUserDto("Jane Bis", "jane@contoso.com", "AutreMotDePasse#1"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsToken()
    {
        var sut = CreateSut(out _);
        await sut.RegisterAsync(new RegisterUserDto("Jane Doe", "jane@contoso.com", "MotDePasse#2026"));

        var result = await sut.LoginAsync(new LoginRequestDto("jane@contoso.com", "MotDePasse#2026"));

        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var sut = CreateSut(out _);
        await sut.RegisterAsync(new RegisterUserDto("Jane Doe", "jane@contoso.com", "MotDePasse#2026"));

        var result = await sut.LoginAsync(new LoginRequestDto("jane@contoso.com", "MauvaisMotDePasse"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ReturnsNull()
    {
        var sut = CreateSut(out _);

        var result = await sut.LoginAsync(new LoginRequestDto("inconnu@contoso.com", "peu importe"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_ReturnsNull()
    {
        var sut = CreateSut(out var db);
        await sut.RegisterAsync(new RegisterUserDto("Jane Doe", "jane@contoso.com", "MotDePasse#2026"));

        var user = db.Users.First();
        user.IsActive = false;
        db.SaveChanges();

        var result = await sut.LoginAsync(new LoginRequestDto("jane@contoso.com", "MotDePasse#2026"));

        result.Should().BeNull();
    }
}
