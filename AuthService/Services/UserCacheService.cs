using AuthService.Models;

namespace AuthService.Services
{
    public class UserCacheService : IUserCacheService
    {
        private readonly IRedisService _redisService;
        private const string USER_PREFIX = "user:";
        private const string USER_EMAIL_PREFIX = "user_email:";
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromMinutes(30);

        public UserCacheService(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _redisService.GetAsync<User>($"{USER_PREFIX}{userId}");
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var userId = await _redisService.GetAsync<Guid>($"{USER_EMAIL_PREFIX}{email}");
            if (userId == Guid.Empty)
                return null;

            return await GetUserByIdAsync(userId);
        }

        public async Task SetUserAsync(User user, TimeSpan? expiry = null)
        {
            var userKey = $"{USER_PREFIX}{user.Id}";
            var emailKey = $"{USER_EMAIL_PREFIX}{user.Email}";

            await _redisService.SetAsync(userKey, user, expiry ?? _defaultExpiry);
            await _redisService.SetAsync(emailKey, user.Id, expiry ?? _defaultExpiry);
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
                return false;

            var userKey = $"{USER_PREFIX}{userId}";
            var emailKey = $"{USER_EMAIL_PREFIX}{user.Email}";

            await _redisService.DeleteAsync(userKey);
            await _redisService.DeleteAsync(emailKey);

            return true;
        }

        public async Task<bool> DeleteUserByEmailAsync(string email)
        {
            var userId = await _redisService.GetAsync<Guid>($"{USER_EMAIL_PREFIX}{email}");
            if (userId == Guid.Empty)
                return false;

            return await DeleteUserAsync(userId);
        }

        public async Task<bool> ExistsAsync(Guid userId)
        {
            return await _redisService.ExistsAsync($"{USER_PREFIX}{userId}");
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _redisService.ExistsAsync($"{USER_EMAIL_PREFIX}{email}");
        }
    }
} 