using AuthService.Models;

namespace AuthService.Services
{
    public class UserCacheService : IUserCacheService
    {
        private readonly ICacheService _cacheService;
        private const string UserPrefix = "user:";
        private const string EmailPrefix = "email:";

        public UserCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _cacheService.GetAsync<User>(EmailPrefix + email);
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _cacheService.GetAsync<User>(UserPrefix + userId);
        }

        public async Task SetUserAsync(User user, TimeSpan? expiry = null)
        {
            await _cacheService.SetAsync(UserPrefix + user.Id, user, expiry);
            await _cacheService.SetAsync(EmailPrefix + user.Email, user, expiry);
        }

        public async Task<bool> DeleteUserByEmailAsync(string email)
        {
            var user = await GetUserByEmailAsync(email);
            if (user != null)
            {
                await _cacheService.DeleteAsync(UserPrefix + user.Id);
                await _cacheService.DeleteAsync(EmailPrefix + email);
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
            {
                await _cacheService.DeleteAsync(UserPrefix + userId);
                await _cacheService.DeleteAsync(EmailPrefix + user.Email);
                return true;
            }
            return false;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _cacheService.ExistsAsync(EmailPrefix + email);
        }

        public async Task<bool> ExistsAsync(Guid userId)
        {
            return await _cacheService.ExistsAsync(UserPrefix + userId);
        }
    }
} 