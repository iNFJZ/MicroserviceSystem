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
        private readonly Mock<IRedisService> _redisServiceMock;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _redisServiceMock = new Mock<IRedisService>();
            _sessionService = new SessionService(_redisServiceMock.Object);
        }

        [Fact]
        public async Task IsTokenBlacklistedAsync_ReturnsTrue_WhenTokenExists()
        {
            // Arrange
            var token = "test-token";
            _redisServiceMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

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
            _redisServiceMock.Verify(r => r.SetAsync(It.IsAny<string>(), true, expiry), Times.Once);
        }

        [Fact]
        public async Task IsUserSessionValidAsync_ReturnsTrue_WhenSessionExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var sessionId = "session-1";
            _redisServiceMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

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
            _redisServiceMock.Verify(r => r.SetAsync(It.IsAny<string>(), It.IsAny<object>(), expiry), Times.Once);
            _redisServiceMock.Verify(r => r.SetHashAsync(It.IsAny<string>(), sessionId, It.IsAny<string>()), Times.Once);
            _redisServiceMock.Verify(r => r.SetExpiryAsync(It.IsAny<string>(), expiry), Times.Once);
        }

        [Fact]
        public async Task RemoveUserSessionAsync_CallsDeleteAsyncAndDeleteHashAsync()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var sessionId = "session-1";
            _redisServiceMock.Setup(r => r.ExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            // Act
            var result = await _sessionService.RemoveUserSessionAsync(userId, sessionId);

            // Assert
            Assert.True(result);
            _redisServiceMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Once);
            _redisServiceMock.Verify(r => r.DeleteHashAsync(It.IsAny<string>(), sessionId), Times.Once);
        }
    }
} 