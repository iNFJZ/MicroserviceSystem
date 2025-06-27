using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Services;
using AuthService.DTOs;
using AuthService.Exceptions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly AuthService.Services.AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _configMock = new Mock<IConfiguration>();
        _sessionServiceMock = new Mock<ISessionService>();
        _configMock.Setup(c => c["JwtSettings:Key"]).Returns("supersecretkeysupersecretkey123456");
        _configMock.Setup(c => c["JwtSettings:Issuer"]).Returns("issuer");
        _configMock.Setup(c => c["JwtSettings:Audience"]).Returns("audience");
        _authService = new AuthService.Services.AuthService(_userRepoMock.Object, _configMock.Object, _sessionServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldAddUserAndReturnToken()
    {
        // Arrange
        var dto = new RegisterDto { Username = "user", Email = "user@email.com", Password = "pass" };
        _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.CreateUserSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.SetUserLoginStatusAsync(It.IsAny<Guid>(), true, It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

        // Act
        var token = await _authService.RegisterAsync(dto);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _sessionServiceMock.Verify(s => s.CreateUserSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _sessionServiceMock.Verify(s => s.SetUserLoginStatusAsync(It.IsAny<Guid>(), true, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
    {
        // Arrange
        var password = "pass";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = new User { Id = Guid.NewGuid(), Email = "user@email.com", Username = "user", PasswordHash = hash };
        var dto = new LoginDto { Email = user.Email, Password = password };
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _sessionServiceMock.Setup(s => s.CreateUserSessionAsync(user.Id, It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.SetUserLoginStatusAsync(user.Id, true, It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

        // Act
        var token = await _authService.LoginAsync(dto);

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        _sessionServiceMock.Verify(s => s.CreateUserSessionAsync(user.Id, It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _sessionServiceMock.Verify(s => s.SetUserLoginStatusAsync(user.Id, true, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenCredentialsInvalid()
    {
        // Arrange
        var dto = new LoginDto { Email = "notfound@email.com", Password = "wrong" };
        _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => _authService.LoginAsync(dto));
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnTrue_WhenTokenValid()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "user@email.com", Username = "user" };
        var token = _authService.GetType().GetMethod("GenerateJwt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_authService, new object[] { user }) as string ?? string.Empty;
        _sessionServiceMock.Setup(s => s.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.SetUserLoginStatusAsync(user.Id, false, null)).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LogoutAsync(token);

        // Assert
        Assert.True(result);
        _sessionServiceMock.Verify(s => s.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _sessionServiceMock.Verify(s => s.SetUserLoginStatusAsync(user.Id, false, null), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_ShouldReturnFalse_IfTokenBlacklisted()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Email = "user@email.com", Username = "user" };
        var token = _authService.GetType().GetMethod("GenerateJwt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_authService, new object[] { user }) as string ?? string.Empty;
        _sessionServiceMock.Setup(s => s.IsTokenBlacklistedAsync(token)).ReturnsAsync(true);

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }
} 