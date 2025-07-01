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
        private const string ActiveTokenPrefix = "token:";
        private const string ResetTokenPrefix = "reset:";
        private const string UserLockPrefix = "lock:";
        private const string FailedAttemptsPrefix = "failed:";

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

        public async Task StoreActiveTokenAsync(string token, Guid userId, TimeSpan expiry)
        {
            var tokenKey = ActiveTokenPrefix + token;
            await _cacheService.SetAsync(tokenKey, userId.ToString(), expiry);
        }

        public async Task<bool> IsTokenActiveAsync(string token)
        {
            var tokenKey = ActiveTokenPrefix + token;
            return await _cacheService.ExistsAsync(tokenKey);
        }

        public async Task RemoveActiveTokenAsync(string token)
        {
            var tokenKey = ActiveTokenPrefix + token;
            await _cacheService.DeleteAsync(tokenKey);
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

        public async Task<Guid?> GetUserIdFromActiveTokenAsync(string token)
        {
            var tokenKey = ActiveTokenPrefix + token;
            var userIdString = await _cacheService.GetAsync<string>(tokenKey);
            if (Guid.TryParse(userIdString, out Guid userId))
                return userId;
            return null;
        }

        public async Task StoreResetTokenAsync(string token, Guid userId, TimeSpan expiry)
        {
            var tokenKey = ResetTokenPrefix + token;
            await _cacheService.SetAsync(tokenKey, userId.ToString(), expiry);
        }

        public async Task<Guid?> GetUserIdFromResetTokenAsync(string token)
        {
            var tokenKey = ResetTokenPrefix + token;
            var userIdString = await _cacheService.GetAsync<string>(tokenKey);
            if (Guid.TryParse(userIdString, out Guid userId))
                return userId;
            return null;
        }

        public async Task RemoveResetTokenAsync(string token)
        {
            var tokenKey = ResetTokenPrefix + token;
            await _cacheService.DeleteAsync(tokenKey);
        }

        public async Task<bool> IsUserLockedAsync(Guid userId)
        {
            var lockKey = UserLockPrefix + userId;
            return await _cacheService.ExistsAsync(lockKey);
        }

        public async Task<DateTime?> GetUserLockExpiryAsync(Guid userId)
        {
            var lockKey = UserLockPrefix + userId;
            var lockExpiryString = await _cacheService.GetAsync<string>(lockKey);
            if (DateTime.TryParse(lockExpiryString, out DateTime lockExpiry))
                return lockExpiry;
            return null;
        }

        public async Task LockUserAsync(Guid userId, DateTime lockExpiry)
        {
            var lockKey = UserLockPrefix + userId;
            var expiry = lockExpiry - DateTime.UtcNow;
            await _cacheService.SetAsync(lockKey, lockExpiry.ToString(), expiry);
        }

        public async Task<int> IncrementFailedLoginAttemptsAsync(Guid userId)
        {
            var failedKey = FailedAttemptsPrefix + userId;
            var currentAttempts = await _cacheService.GetAsync<int>(failedKey);
            var newAttempts = currentAttempts + 1;
            await _cacheService.SetAsync(failedKey, newAttempts, TimeSpan.FromMinutes(30));
            return newAttempts;
        }

        public async Task ResetFailedLoginAttemptsAsync(Guid userId)
        {
            var failedKey = FailedAttemptsPrefix + userId;
            await _cacheService.DeleteAsync(failedKey);
        }
    }
} 