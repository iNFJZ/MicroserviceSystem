using AuthService.Services;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.DTOs;
using AuthService.Exceptions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;

namespace AuthService.Tests
{
    public class TokenValidationTests
    {
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IPasswordService> _mockPasswordService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailMessageService> _emailMessageServiceMock = new Mock<IEmailMessageService>();
        private readonly Mock<IHunterEmailVerifierService> _emailVerifierServiceMock = new Mock<IHunterEmailVerifierService>();
        private readonly ILogger<AuthService.Services.AuthService> _logger = new LoggerFactory().CreateLogger<AuthService.Services.AuthService>();
        private readonly AuthService.Services.AuthService _authService;

        public TokenValidationTests()
        {
            _mockUserRepo = new Mock<IUserRepository>();
            _mockSessionService = new Mock<ISessionService>();
            _mockJwtService = new Mock<IJwtService>();
            _mockPasswordService = new Mock<IPasswordService>();
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(c => c["AuthPolicy:MaxFailedLoginAttempts"]).Returns("3");
            _mockConfig.Setup(c => c["AuthPolicy:AccountLockMinutes"]).Returns("5");
            _mockConfig.Setup(c => c["AuthPolicy:ResetPasswordTokenExpiryMinutes"]).Returns("15");
            _authService = new AuthService.Services.AuthService(
                _mockUserRepo.Object,
                _mockSessionService.Object,
                _mockJwtService.Object,
                _mockPasswordService.Object,
                _logger,
                _emailMessageServiceMock.Object,
                _mockConfig.Object,
                _emailVerifierServiceMock.Object
            );
        }

        [Fact]
        public async Task LoginAsync_ShouldStoreTokenInRedis()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "password" };
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Username = "testuser" };
            var token = "test.jwt.token";
            var tokenExpiry = TimeSpan.FromMinutes(60);

            _mockUserRepo.Setup(x => x.GetByEmailAsync(loginDto.Email)).ReturnsAsync(user);
            _mockPasswordService.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash)).Returns(true);
            _mockJwtService.Setup(x => x.GenerateToken(user)).Returns(token);
            _mockJwtService.Setup(x => x.GetTokenExpirationTimeSpan(token)).Returns(tokenExpiry);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Equal(token, result);
            _mockSessionService.Verify(x => x.StoreActiveTokenAsync(token, user.Id, tokenExpiry), Times.Once);
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnFalse_WhenTokenNotInRedis()
        {
            // Arrange
            var token = "test.jwt.token";
            _mockSessionService.Setup(x => x.IsTokenBlacklistedAsync(token)).ReturnsAsync(false);
            _mockSessionService.Setup(x => x.IsTokenActiveAsync(token)).ReturnsAsync(false);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            Assert.False(result);
            _mockJwtService.Verify(x => x.ValidateToken(token), Times.Never);
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnFalse_WhenTokenBlacklisted()
        {
            // Arrange
            var token = "test.jwt.token";
            _mockSessionService.Setup(x => x.IsTokenBlacklistedAsync(token)).ReturnsAsync(true);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            Assert.False(result);
            _mockSessionService.Verify(x => x.IsTokenActiveAsync(token), Times.Never);
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnTrue_WhenTokenValidAndActive()
        {
            // Arrange
            var token = "test.jwt.token";
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Email = "test@example.com", Username = "testuser" };

            _mockSessionService.Setup(x => x.IsTokenBlacklistedAsync(token)).ReturnsAsync(false);
            _mockSessionService.Setup(x => x.IsTokenActiveAsync(token)).ReturnsAsync(true);
            _mockJwtService.Setup(x => x.ValidateToken(token)).Returns(true);
            _mockJwtService.Setup(x => x.GetUserIdFromToken(token)).Returns(userId);
            _mockUserRepo.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task LogoutAsync_ShouldRemoveTokenFromRedis()
        {
            // Arrange
            var token = "test.jwt.token";
            var userId = Guid.NewGuid();
            var tokenExpiry = TimeSpan.FromMinutes(60);

            _mockJwtService.Setup(x => x.GetUserIdFromToken(token)).Returns(userId);
            _mockJwtService.Setup(x => x.GetTokenExpirationTimeSpan(token)).Returns(tokenExpiry);

            // Act
            var result = await _authService.LogoutAsync(token);

            // Assert
            Assert.True(result);
            _mockSessionService.Verify(x => x.RemoveActiveTokenAsync(token), Times.Once);
            _mockSessionService.Verify(x => x.BlacklistTokenAsync(token, tokenExpiry), Times.Once);
            _mockSessionService.Verify(x => x.SetUserLoginStatusAsync(userId, false, null), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_ShouldReturnFalse_WhenInvalidToken()
        {
            // Arrange
            var token = "invalid.token";
            _mockJwtService.Setup(x => x.GetUserIdFromToken(token)).Returns((Guid?)null);

            // Act
            var result = await _authService.LogoutAsync(token);

            // Assert
            Assert.False(result);
            _mockSessionService.Verify(x => x.RemoveActiveTokenAsync(token), Times.Never);
        }
    }
} 