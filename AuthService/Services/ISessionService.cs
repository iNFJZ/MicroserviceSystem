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
        Task<Guid?> GetUserIdFromActiveTokenAsync(string token);
    }
} 