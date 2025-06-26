using AuthService.Models;

namespace AuthService.Services
{
    public interface IUserCacheService
    {
        Task<User?> GetUserByIdAsync(Guid userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task SetUserAsync(User user, TimeSpan? expiry = null);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> DeleteUserByEmailAsync(string email);
        Task<bool> ExistsAsync(Guid userId);
        Task<bool> ExistsByEmailAsync(string email);
    }
} 