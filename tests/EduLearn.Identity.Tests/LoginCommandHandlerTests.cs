using Moq;
using FluentAssertions;
using EduLearn.Identity.API.Application.Commands;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Identity.API.Domain.Entities;
using EduLearn.Identity.API.Domain.Enums;
using EduLearn.Shared.Exceptions;

namespace EduLearn.Identity.Tests;

// Unit tests for LoginCommandHandler
public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository>        _userRepoMock  = new();
    private readonly Mock<IRefreshTokenRepository> _tokenRepoMock = new();
    private readonly Mock<IJwtService>            _jwtMock       = new();

    private LoginCommandHandler CreateHandler() =>
        new(_userRepoMock.Object, _tokenRepoMock.Object, _jwtMock.Object);

    // Helper: create a user with a known bcrypt hash for "Password1!"
    private static User CreateTestUser()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("Password1!", workFactor: 4); // Low cost for speed
        return User.Create("Test User", "user@example.com", hash, UserRole.Student);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var user = CreateTestUser();
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>()));
        _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _tokenRepoMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("mock.jwt.token");
        _jwtMock.Setup(j => j.GenerateRefreshToken()).Returns("raw-refresh-token");
        _jwtMock.Setup(j => j.HashRefreshToken(It.IsAny<string>())).Returns("hashed-token");

        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "Password1!");

        // Act
        var (authResponse, rawToken) = await handler.Handle(command, CancellationToken.None);

        // Assert: access token returned
        authResponse.AccessToken.Should().Be("mock.jwt.token");
        authResponse.ExpiresInSeconds.Should().Be(900);
        rawToken.Should().Be("raw-refresh-token");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldThrowUnauthorized()
    {
        // Arrange: user exists but password won't match "WrongPassword"
        var user = CreateTestUser();
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "WrongPassword");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ShouldThrowUnauthorized()
    {
        // Arrange: no user found
        _userRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var command = new LoginCommand("ghost@example.com", "Password1!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithBannedUser_ShouldThrowForbidden()
    {
        // Arrange: user is banned
        var user = CreateTestUser();
        user.Ban(); // OOP domain method
        _userRepoMock.Setup(r => r.GetByEmailAsync("user@example.com")).ReturnsAsync(user);

        var handler = CreateHandler();
        var command = new LoginCommand("user@example.com", "Password1!");

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
