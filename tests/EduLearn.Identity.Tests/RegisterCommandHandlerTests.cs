using Moq;
using FluentAssertions;
using EduLearn.Identity.API.Application.Commands;
using EduLearn.Identity.API.Application.Interfaces;
using EduLearn.Identity.API.Domain.Entities;
using EduLearn.Identity.API.Domain.Enums;
using EduLearn.Shared.Exceptions;
using MassTransit;

namespace EduLearn.Identity.Tests;

// Unit tests for RegisterCommandHandler
// Uses Moq to mock all dependencies — no real DB or RabbitMQ needed
public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository>   _userRepoMock = new();
    private readonly Mock<IPublishEndpoint>  _busMock      = new();

    private RegisterCommandHandler CreateHandler() =>
        new(_userRepoMock.Object, _busMock.Object);

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUser()
    {
        // Arrange: email is not taken
        _userRepoMock.Setup(r => r.EmailExistsAsync("test@example.com")).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _busMock.Setup(b => b.Publish(It.IsAny<EduLearn.Shared.Events.UserRegisteredEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var command = new RegisterCommand("John Doe", "test@example.com", "Password1!", "Student");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.Role.Should().Be("Student");

        // Verify user was saved and event was published
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _busMock.Verify(b => b.Publish(It.IsAny<EduLearn.Shared.Events.UserRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldThrowConflictException()
    {
        // Arrange: email already exists
        _userRepoMock.Setup(r => r.EmailExistsAsync("taken@example.com")).ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new RegisterCommand("Jane Doe", "taken@example.com", "Password1!", "Student");

        // Act & Assert: should throw ConflictException
        await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Verify no user was saved
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ShouldThrowArgumentException()
    {
        _userRepoMock.Setup(r => r.EmailExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new RegisterCommand("Bob", "bob@example.com", "Password1!", "SuperAdmin");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
