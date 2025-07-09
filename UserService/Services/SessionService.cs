using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using UserService.Services;

namespace UserService.Services
{
    public class SessionService : ISessionService
    {
        private readonly ICacheService _cacheService;
        private readonly IHashService _hashService;
        private readonly IRedisKeyService _keyService;

        private const string UserSessionPrefix = "session:";
        private const string UserLoginPrefix = "login:";
        private const string ActiveTokenPrefix = "token:";

        public SessionService(ICacheService cacheService, IHashService hashService, IRedisKeyService keyService)
        {
            _cacheService = cacheService;
            _hashService = hashService;
            _keyService = keyService;
        }

        public async Task RemoveAllUserSessionsAsync(Guid userId)
        {
            var sessionKey = UserSessionPrefix + userId;
            await _cacheService.DeleteAsync(sessionKey);
        }

        public async Task RemoveAllActiveTokensForUserAsync(Guid userId)
        {
            var pattern = ActiveTokenPrefix + "*";
            var keys = await _cacheService.GetKeysAsync(pattern);
            
            foreach (var key in keys)
            {
                var tokenUserIdString = await _cacheService.GetAsync<string>(key);
                if (Guid.TryParse(tokenUserIdString, out Guid tokenUserId) && tokenUserId == userId)
                {
                    await _cacheService.DeleteAsync(key);
                }
            }
        }

        public async Task SetUserLoginStatusAsync(Guid userId, bool isLoggedIn, TimeSpan? expiry = null)
        {
            var loginKey = UserLoginPrefix + userId;
            if (isLoggedIn)
            {
                await _cacheService.SetAsync(loginKey, true, expiry);
            }
            else
            {
                await _cacheService.DeleteAsync(loginKey);
            }
        }
    }
} 