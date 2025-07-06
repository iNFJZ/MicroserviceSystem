using System;
using System.Threading.Tasks;
using AuthService.Models;
using AuthService.Services;
using Moq;
using Xunit;

namespace AuthService.Tests
{
    public class UserCacheServiceTests
    {
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly UserCacheService _userCacheService;
        private readonly User _testUser;

        public UserCacheServiceTests()
        {
            _cacheServiceMock = new Mock<ICacheService>();
            _userCacheService = new UserCacheService(_cacheServiceMock.Object);
            _testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsUser_WhenUserExistsInCache()
        {
            var userId = _testUser.Id;
            _cacheServiceMock.Setup(r => r.GetAsync<User>($"user:{userId}"))
                .ReturnsAsync(_testUser);

            var result = await _userCacheService.GetUserByIdAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(_testUser.Id, result.Id);
            Assert.Equal(_testUser.Email, result.Email);
            _cacheServiceMock.Verify(r => r.GetAsync<User>($"user:{userId}"), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_ReturnsNull_WhenUserNotInCache()
        {
            var userId = Guid.NewGuid();
            _cacheServiceMock.Setup(r => r.GetAsync<User>($"user:{userId}"))
                .ReturnsAsync((User?)null);

            var result = await _userCacheService.GetUserByIdAsync(userId);

            Assert.Null(result);
            _cacheServiceMock.Verify(r => r.GetAsync<User>($"user:{userId}"), Times.Once);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsUser_WhenUserExistsInCache()
        {
            var email = _testUser.Email;
            _cacheServiceMock.Setup(r => r.GetAsync<User>($"email:{email}"))
                .ReturnsAsync(_testUser);

            var result = await _userCacheService.GetUserByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Equal(_testUser.Id, result.Id);
            Assert.Equal(_testUser.Email, result.Email);
            _cacheServiceMock.Verify(r => r.GetAsync<User>($"email:{email}"), Times.Once);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ReturnsNull_WhenEmailNotInCache()
        {
            var email = "nonexistent@example.com";
            _cacheServiceMock.Setup(r => r.GetAsync<User>($"email:{email}"))
                .ReturnsAsync((User?)null);

            var result = await _userCacheService.GetUserByEmailAsync(email);

            Assert.Null(result);
            _cacheServiceMock.Verify(r => r.GetAsync<User>($"email:{email}"), Times.Once);
        }

        [Fact]
        public async Task SetUserAsync_CallsSetAsyncForBothUserAndEmail()
        {
            var user = _testUser;

            await _userCacheService.SetUserAsync(user);

            _cacheServiceMock.Verify(r => r.SetAsync($"user:{user.Id}", user, null), Times.Once);
            _cacheServiceMock.Verify(r => r.SetAsync($"email:{user.Email}", user, null), Times.Once);
        }

        [Fact]
        public async Task SetUserAsync_WithCustomExpiry_UsesProvidedExpiry()
        {
            var user = _testUser;
            var customExpiry = TimeSpan.FromHours(2);

            await _userCacheService.SetUserAsync(user, customExpiry);

            _cacheServiceMock.Verify(r => r.SetAsync($"user:{user.Id}", user, customExpiry), Times.Once);
            _cacheServiceMock.Verify(r => r.SetAsync($"email:{user.Email}", user, customExpiry), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_DeletesBothUserAndEmailKeys()
        {
            var userId = _testUser.Id;
            _cacheServiceMock.Setup(r => r.GetAsync<User>($"user:{userId}"))
                .ReturnsAsync(_testUser);

            var result = await _userCacheService.DeleteUserAsync(userId);

            Assert.True(result);
            _cacheServiceMock.Verify(r => r.DeleteAsync($"user:{userId}"), Times.Once);
            _cacheServiceMock.Verify(r => r.DeleteAsync($"email:{_testUser.Email}"), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_ReturnsFalse_WhenUserNotInCache()
        {
            var userId = Guid.NewGuid();
            _cacheServiceMock.Setup(r => r.GetAsync<User>($"user:{userId}"))
                .ReturnsAsync((User?)null);

            var result = await _userCacheService.DeleteUserAsync(userId);

            Assert.False(result);
            _cacheServiceMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrue_WhenUserExists()
        {
            var userId = _testUser.Id;
            _cacheServiceMock.Setup(r => r.ExistsAsync($"user:{userId}"))
                .ReturnsAsync(true);

            var result = await _userCacheService.ExistsAsync(userId);

            Assert.True(result);
            _cacheServiceMock.Verify(r => r.ExistsAsync($"user:{userId}"), Times.Once);
        }

        [Fact]
        public async Task ExistsByEmailAsync_ReturnsTrue_WhenEmailExists()
        {
            var email = _testUser.Email;
            _cacheServiceMock.Setup(r => r.ExistsAsync($"email:{email}"))
                .ReturnsAsync(true);

            var result = await _userCacheService.ExistsByEmailAsync(email);

            Assert.True(result);
            _cacheServiceMock.Verify(r => r.ExistsAsync($"email:{email}"), Times.Once);
        }
    }
} 