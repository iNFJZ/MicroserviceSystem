using AuthService.Services;

namespace AuthService.Services
{
    public class SessionService : ISessionService
    {
        private readonly ICacheService _cacheService;
        private readonly IHashService _hashService;
        private readonly IRedisKeyService _keyService;

        private const string TokenBlacklistPrefix = "blacklist:";
        private const string UserSessionPrefix = "session:";
        private const string UserLoginPrefix = "login:";

        public SessionService(ICacheService cacheService, IHashService hashService, IRedisKeyService keyService)
        {
            _cacheService = cacheService;
            _hashService = hashService;
            _keyService = keyService;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            return await _cacheService.ExistsAsync(TokenBlacklistPrefix + token);
        }

        public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
        {
            await _cacheService.SetAsync(TokenBlacklistPrefix + token, true, expiry);
        }

        public async Task<bool> IsUserSessionValidAsync(Guid userId, string sessionId)
        {
            var sessionKey = UserSessionPrefix + userId;
            var session = await _hashService.GetHashAsync(sessionKey, sessionId);
            return !string.IsNullOrEmpty(session);
        }

        public async Task CreateUserSessionAsync(Guid userId, string sessionId, TimeSpan expiry)
        {
            var sessionKey = UserSessionPrefix + userId;
            await _hashService.SetHashAsync(sessionKey, sessionId, DateTime.UtcNow.ToString());
            await _cacheService.SetExpiryAsync(sessionKey, expiry);
        }

        public async Task<bool> RemoveUserSessionAsync(Guid userId, string sessionId)
        {
            var sessionKey = UserSessionPrefix + userId;
            return await _hashService.DeleteHashAsync(sessionKey, sessionId);
        }

        public async Task RemoveAllUserSessionsAsync(Guid userId)
        {
            var sessionKey = UserSessionPrefix + userId;
            await _cacheService.DeleteAsync(sessionKey);
        }

        public async Task<IEnumerable<string>> GetUserSessionsAsync(Guid userId)
        {
            var sessionKey = UserSessionPrefix + userId;
            var sessions = await _hashService.GetHashAllAsync(sessionKey);
            return sessions.Keys;
        }

        public async Task<bool> IsUserLoggedInAsync(Guid userId)
        {
            var loginKey = UserLoginPrefix + userId;
            var status = await _cacheService.GetAsync<bool>(loginKey);
            return status;
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