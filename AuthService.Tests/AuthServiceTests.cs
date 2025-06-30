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
using Microsoft.Extensions.Logging;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IEmailMessageService> _emailMessageServiceMock = new Mock<IEmailMessageService>();
    private readonly ILogger<AuthService.Services.AuthService> _logger = new LoggerFactory().CreateLogger<AuthService.Services.AuthService>();
    private readonly AuthService.Services.AuthService _authService;

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _configMock = new Mock<IConfiguration>();
        _sessionServiceMock = new Mock<ISessionService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _passwordServiceMock = new Mock<IPasswordService>();
        
        _configMock.Setup(c => c["JwtSettings:Key"]).Returns("supersecretkeysupersecretkey123456");
        _configMock.Setup(c => c["JwtSettings:Issuer"]).Returns("issuer");
        _configMock.Setup(c => c["JwtSettings:Audience"]).Returns("audience");
        
        _authService = new AuthService.Services.AuthService(
            _userRepoMock.Object, 
            _sessionServiceMock.Object, 
            _jwtServiceMock.Object, 
            _passwordServiceMock.Object,
            _logger,
            _emailMessageServiceMock.Object
        );
    }

    [Fact]
    public async Task RegisterAsync_ShouldAddUserAndReturnToken()
    {
        // Arrange
        var dto = new RegisterDto { Username = "user", Email = "user@email.com", Password = "pass" };
        var user = new User { Id = Guid.NewGuid(), Email = dto.Email, Username = dto.Username };
        var token = "test.jwt.token";
        var tokenExpiry = TimeSpan.FromMinutes(60);
        
        _userRepoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _passwordServiceMock.Setup(p => p.HashPassword(dto.Password)).Returns("hashedpassword");
        _jwtServiceMock.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(token);
        _jwtServiceMock.Setup(j => j.GetTokenExpirationTimeSpan(token)).Returns(tokenExpiry);
        _sessionServiceMock.Setup(s => s.StoreActiveTokenAsync(token, It.IsAny<Guid>(), tokenExpiry)).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.CreateUserSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.SetUserLoginStatusAsync(It.IsAny<Guid>(), true, It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        Assert.Equal(token, result);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _sessionServiceMock.Verify(s => s.StoreActiveTokenAsync(token, It.IsAny<Guid>(), tokenExpiry), Times.Once);
        _sessionServiceMock.Verify(s => s.CreateUserSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _sessionServiceMock.Verify(s => s.SetUserLoginStatusAsync(It.IsAny<Guid>(), true, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
    {
        // Arrange
        var password = "pass";
        var user = new User { Id = Guid.NewGuid(), Email = "user@email.com", Username = "user", PasswordHash = "hashedpassword" };
        var dto = new LoginDto { Email = user.Email, Password = password };
        var token = "test.jwt.token";
        var tokenExpiry = TimeSpan.FromMinutes(60);
        
        _userRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword(password, user.PasswordHash)).Returns(true);
        _jwtServiceMock.Setup(j => j.GenerateToken(user)).Returns(token);
        _jwtServiceMock.Setup(j => j.GetTokenExpirationTimeSpan(token)).Returns(tokenExpiry);
        _sessionServiceMock.Setup(s => s.StoreActiveTokenAsync(token, user.Id, tokenExpiry)).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.CreateUserSessionAsync(user.Id, It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.SetUserLoginStatusAsync(user.Id, true, It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        Assert.Equal(token, result);
        _sessionServiceMock.Verify(s => s.StoreActiveTokenAsync(token, user.Id, tokenExpiry), Times.Once);
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
        var userId = Guid.NewGuid();
        var token = "test.jwt.token";
        var tokenExpiry = TimeSpan.FromMinutes(60);
        
        _jwtServiceMock.Setup(j => j.GetUserIdFromToken(token)).Returns(userId);
        _jwtServiceMock.Setup(j => j.GetTokenExpirationTimeSpan(token)).Returns(tokenExpiry);
        _sessionServiceMock.Setup(s => s.RemoveActiveTokenAsync(token)).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(s => s.SetUserLoginStatusAsync(userId, false, null)).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LogoutAsync(token);

        // Assert
        Assert.True(result);
        _sessionServiceMock.Verify(s => s.RemoveActiveTokenAsync(token), Times.Once);
        _sessionServiceMock.Verify(s => s.BlacklistTokenAsync(token, tokenExpiry), Times.Once);
        _sessionServiceMock.Verify(s => s.SetUserLoginStatusAsync(userId, false, null), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_ShouldReturnFalse_IfTokenBlacklisted()
    {
        // Arrange
        var token = "test.jwt.token";
        _sessionServiceMock.Setup(s => s.IsTokenBlacklistedAsync(token)).ReturnsAsync(true);

        // Act
        var result = await _authService.ValidateTokenAsync(token);

        // Assert
        Assert.False(result);
    }
} 