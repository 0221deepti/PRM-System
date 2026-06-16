using FluentAssertions;
using Moq;
using PRM.Application.Interfaces.Repositories;
using PRM.Application.Interfaces.Services;
using PRM.Application.Services;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _tokenServiceMock = new Mock<ITokenService>();

        _authService = new AuthService(
            _userRepoMock.Object,
            _employeeRepoMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponseDto()
    {
        // Arrange
        var password = "Password123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Id = 1, Username = "testuser", PasswordHash = hashedPassword, IsActive = true };
        var employee = new User { Id = 101 };

        _userRepoMock.Setup(repo => repo.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _employeeRepoMock.Setup(repo => repo.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);
        _tokenServiceMock.Setup(s => s.GenerateToken(user, 101))
            .Returns("valid.jwt.token");

        // Act
        var result = await _authService.LoginAsync("testuser", password, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("valid.jwt.token");
        result.UserId.Should().Be(1);
        result.EmployeeId.Should().Be(101);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsDomainException()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1!"), IsActive = true };

        _userRepoMock.Setup(repo => repo.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> action = async () => await _authService.LoginAsync("testuser", "WrongPass1!", CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid credentials.");
    }

    [Fact]
    public async Task LoginAsync_UserInactive_ThrowsDomainException()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass1!"), IsActive = false };

        _userRepoMock.Setup(repo => repo.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> action = async () => await _authService.LoginAsync("testuser", "Pass1!", CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Account is deactivated.");
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("NoDigit!!!")]
    [InlineData("alllowercase1!")]
    public async Task ChangePasswordAsync_WeakPassword_ThrowsDomainException(string weakPassword)
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser", PasswordHash = "oldHash" };
        _userRepoMock.Setup(repo => repo.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> action = async () => await _authService.ChangePasswordAsync(1, weakPassword, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Password must be 8+ characters with at least one uppercase letter and one digit.");
    }
}
