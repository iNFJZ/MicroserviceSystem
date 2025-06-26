using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services
{
    public class SessionService : ISessionService
    {
        private readonly IRedisService _redisService;
        private const string BLACKLIST_PREFIX = "blacklist:";
        private const string SESSION_PREFIX = "session:";
        private const string USER_SESSIONS_PREFIX = "user_sessions:";
        private const string LOGIN_STATUS_PREFIX = "login_status:";

        public SessionService(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            var tokenHash = HashToken(token);
            return await _redisService.ExistsAsync($"{BLACKLIST_PREFIX}{tokenHash}");
        }

        public async Task BlacklistTokenAsync(string token, TimeSpan expiry)
        {
            var tokenHash = HashToken(token);
            await _redisService.SetAsync($"{BLACKLIST_PREFIX}{tokenHash}", true, expiry);
        }

        public async Task<bool> IsUserSessionValidAsync(Guid userId, string sessionId)
        {
            var sessionKey = $"{SESSION_PREFIX}{userId}:{sessionId}";
            return await _redisService.ExistsAsync(sessionKey);
        }

        public async Task CreateUserSessionAsync(Guid userId, string sessionId, TimeSpan expiry)
        {
            var sessionKey = $"{SESSION_PREFIX}{userId}:{sessionId}";
            var userSessionsKey = $"{USER_SESSIONS_PREFIX}{userId}";

            // Store session data
            await _redisService.SetAsync(sessionKey, new { UserId = userId, SessionId = sessionId, CreatedAt = DateTime.UtcNow }, expiry);
            
            // Add session to user's session list
            await _redisService.SetHashAsync(userSessionsKey, sessionId, DateTime.UtcNow.ToString("O"));
            await _redisService.SetExpiryAsync(userSessionsKey, expiry);
        }

        public async Task<bool> RemoveUserSessionAsync(Guid userId, string sessionId)
        {
            var sessionKey = $"{SESSION_PREFIX}{userId}:{sessionId}";
            var userSessionsKey = $"{USER_SESSIONS_PREFIX}{userId}";

            var existed = await _redisService.ExistsAsync(sessionKey);
            await _redisService.DeleteAsync(sessionKey);
            await _redisService.DeleteHashAsync(userSessionsKey, sessionId);
            return existed;
        }

        public async Task RemoveAllUserSessionsAsync(Guid userId)
        {
            var userSessionsKey = $"{USER_SESSIONS_PREFIX}{userId}";
            var sessions = await _redisService.GetHashAllAsync(userSessionsKey);

            foreach (var session in sessions)
            {
                var sessionKey = $"{SESSION_PREFIX}{userId}:{session.Key}";
                await _redisService.DeleteAsync(sessionKey);
            }

            await _redisService.DeleteAsync(userSessionsKey);
        }

        public async Task<IEnumerable<string>> GetUserSessionsAsync(Guid userId)
        {
            var userSessionsKey = $"{USER_SESSIONS_PREFIX}{userId}";
            var sessions = await _redisService.GetHashAllAsync(userSessionsKey);
            return sessions.Keys;
        }

        public async Task<bool> IsUserLoggedInAsync(Guid userId)
        {
            var loginStatusKey = $"{LOGIN_STATUS_PREFIX}{userId}";
            var status = await _redisService.GetAsync<bool>(loginStatusKey);
            return status;
        }

        public async Task SetUserLoginStatusAsync(Guid userId, bool isLoggedIn, TimeSpan? expiry = null)
        {
            var loginStatusKey = $"{LOGIN_STATUS_PREFIX}{userId}";
            if (isLoggedIn)
            {
                await _redisService.SetAsync(loginStatusKey, true, expiry);
            }
            else
            {
                await _redisService.DeleteAsync(loginStatusKey);
            }
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashBytes);
        }
    }
} 