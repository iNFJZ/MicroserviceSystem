namespace AuthService.Services
{
    public interface ISessionService
    {
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task BlacklistTokenAsync(string token, TimeSpan expiry);
        Task<bool> IsUserSessionValidAsync(Guid userId, string sessionId);
        Task CreateUserSessionAsync(Guid userId, string sessionId, TimeSpan expiry);
        Task<bool> RemoveUserSessionAsync(Guid userId, string sessionId);
        Task RemoveAllUserSessionsAsync(Guid userId);
        Task<IEnumerable<string>> GetUserSessionsAsync(Guid userId);
        Task<bool> IsUserLoggedInAsync(Guid userId);
        Task SetUserLoginStatusAsync(Guid userId, bool isLoggedIn, TimeSpan? expiry = null);
        Task StoreActiveTokenAsync(string token, Guid userId, TimeSpan expiry);
        Task<bool> IsTokenActiveAsync(string token);
        Task RemoveActiveTokenAsync(string token);
        Task RemoveAllActiveTokensForUserAsync(Guid userId);
        Task<Guid?> GetUserIdFromActiveTokenAsync(string token);
        Task StoreResetTokenAsync(string token, Guid userId, TimeSpan expiry);
        Task<Guid?> GetUserIdFromResetTokenAsync(string token);
        Task RemoveResetTokenAsync(string token);
        Task<bool> IsUserLockedAsync(Guid userId);
        Task<DateTime?> GetUserLockExpiryAsync(Guid userId);
        Task LockUserAsync(Guid userId, DateTime lockExpiry);
        Task<int> IncrementFailedLoginAttemptsAsync(Guid userId);
        Task ResetFailedLoginAttemptsAsync(Guid userId);
    }
} 