namespace UserService.Services
{
    public interface ISessionService
    {
        Task RemoveAllUserSessionsAsync(Guid userId);
        Task RemoveAllActiveTokensForUserAsync(Guid userId);
        Task SetUserLoginStatusAsync(Guid userId, bool isLoggedIn, TimeSpan? expiry = null);
    }
} 