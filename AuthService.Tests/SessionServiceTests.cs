using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthService.Services;
using Moq;
using Xunit;

namespace AuthService.Tests
{
    public class SessionServiceTests
    {
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<IHashService> _hashServiceMock;
        private readonly Mock<IRedisKeyService> _redisKeyServiceMock;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _cacheServiceMock = new Mock<ICacheService>();
            _hashServiceMock = new Mock<IHashService>();
            _redisKeyServiceMock = new Mock<IRedisKeyService>();
            _sessionService = new SessionService(_cacheServiceMock.Object, _hashServiceMock.Object, _redisKeyServiceMock.Object);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_ReturnsTrue_WhenTokenExists()
        {
            // Arrange
            var token = "test-token";
            _cacheServiceMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await _sessionService.IsTokenBlacklistedAsync(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task BlacklistTokenAsync_CallsSetAsync()
        {
            // Arrange
            var token = "test-token";
            var expiry = TimeSpan.FromMinutes(10);

            // Act
            await _sessionService.BlacklistTokenAsync(token, expiry);

            // Assert
            _cacheServiceMock.Verify(r => r.SetAsync(It.IsAny<string>(), true, expiry), Times.Once);
        }

        [Fact]
        public async Task IsUserSessionValidAsync_ReturnsTrue_WhenSessionExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var sessionId = "session-1";
            _hashServiceMock.Setup(r => r.GetHashAsync(It.IsAny<string>(), sessionId)).ReturnsAsync("session-data");

            // Act
            var result = await _sessionService.IsUserSessionValidAsync(userId, sessionId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateUserSessionAsync_CallsSetAsyncAndSetHashAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var sessionId = "session-1";
            var expiry = TimeSpan.FromMinutes(10);

            // Act
            await _sessionService.CreateUserSessionAsync(userId, sessionId, expiry);

            // Assert
            _hashServiceMock.Verify(r => r.SetHashAsync(It.IsAny<string>(), sessionId, It.IsAny<string>()), Times.Once);
            _cacheServiceMock.Verify(r => r.SetExpiryAsync(It.IsAny<string>(), expiry), Times.Once);
        }

        [Fact]
        public async Task RemoveUserSessionAsync_CallsDeleteAsyncAndDeleteHashAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var sessionId = "session-1";
            _hashServiceMock.Setup(r => r.DeleteHashAsync(It.IsAny<string>(), sessionId)).ReturnsAsync(true);

            // Act
            var result = await _sessionService.RemoveUserSessionAsync(userId, sessionId);

            // Assert
            Assert.True(result);
            _hashServiceMock.Verify(r => r.DeleteHashAsync(It.IsAny<string>(), sessionId), Times.Once);
        }
    }
} 