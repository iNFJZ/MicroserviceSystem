using AuthService.Services;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System.Text.Json;

namespace AuthService.Tests
{
    public class GoogleAuthServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IEmailMessageService> _mockEmailService;
        private readonly Mock<ILogger<GoogleAuthService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly GoogleAuthService _googleAuthService;

        public GoogleAuthServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockSessionService = new Mock<ISessionService>();
            _mockJwtService = new Mock<IJwtService>();
            _mockEmailService = new Mock<IEmailMessageService>();
            _mockLogger = new Mock<ILogger<GoogleAuthService>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockHttpClient = new Mock<HttpClient>();

            _googleAuthService = new GoogleAuthService(
                _mockUserRepository.Object,
                _mockSessionService.Object,
                _mockJwtService.Object,
                _mockEmailService.Object,
                _mockConfig.Object,
                _mockHttpClient.Object
            );
        }

        [Fact]
        public async Task LoginWithGoogleAsync_NewUser_ShouldCreateUserAndReturnToken()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { Code = "valid-code", RedirectUri = "http://localhost:3000/callback" };
            var googleUserInfo = new GoogleUserInfo
            {
                Sub = "google123",
                Email = "test@example.com",
                Name = "Test User",
                GivenName = "Test",
                FamilyName = "User",
                Picture = "https://example.com/picture.jpg",
                EmailVerified = true
            };

            _mockUserRepository.Setup(x => x.GetByGoogleIdAsync("google123"))
                .ReturnsAsync((User?)null);
            _mockUserRepository.Setup(x => x.GetByEmailAsync("test@example.com"))
                .ReturnsAsync((User?)null);

            _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("jwt-token");
            _mockJwtService.Setup(x => x.GetTokenExpirationTimeSpan("jwt-token"))
                .Returns(TimeSpan.FromHours(1));

            // Act
            var result = await _googleAuthService.LoginWithGoogleAsync(googleLoginDto);

            // Assert
            Assert.Equal("jwt-token", result);
            _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
                u.Email == "test@example.com" && 
                u.GoogleId == "google123" && 
                u.LoginProvider == "Google")), Times.Once);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_ExistingUserByGoogleId_ShouldReturnToken()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { Code = "valid-code", RedirectUri = "http://localhost:3000/callback" };
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                GoogleId = "google123",
                LoginProvider = "Google"
            };

            _mockUserRepository.Setup(x => x.GetByGoogleIdAsync("google123"))
                .ReturnsAsync(existingUser);

            _mockJwtService.Setup(x => x.GenerateToken(existingUser))
                .Returns("jwt-token");
            _mockJwtService.Setup(x => x.GetTokenExpirationTimeSpan("jwt-token"))
                .Returns(TimeSpan.FromHours(1));

            // Act
            var result = await _googleAuthService.LoginWithGoogleAsync(googleLoginDto);

            // Assert
            Assert.Equal("jwt-token", result);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task LoginWithGoogleAsync_ExistingUserByEmail_ShouldLinkGoogleAccount()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto { Code = "valid-code", RedirectUri = "http://localhost:3000/callback" };
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                LoginProvider = "Local"
            };

            _mockUserRepository.Setup(x => x.GetByGoogleIdAsync("google123"))
                .ReturnsAsync((User?)null);
            _mockUserRepository.Setup(x => x.GetByEmailAsync("test@example.com"))
                .ReturnsAsync(existingUser);

            _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("jwt-token");
            _mockJwtService.Setup(x => x.GetTokenExpirationTimeSpan("jwt-token"))
                .Returns(TimeSpan.FromHours(1));

            // Act
            var result = await _googleAuthService.LoginWithGoogleAsync(googleLoginDto);

            // Assert
            Assert.Equal("jwt-token", result);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.Is<User>(u => 
                u.GoogleId == "google123" && 
                u.LoginProvider == "Google")), Times.Once);
        }
    }
} 