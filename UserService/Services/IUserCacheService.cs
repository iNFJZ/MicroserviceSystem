using UserService.Models;

namespace UserService.Services
{
    public interface IUserCacheService
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(Guid userId);
        Task SetUserAsync(User user, TimeSpan? expiry = null);
        Task<bool> DeleteUserByEmailAsync(string email);
        Task<bool> DeleteUserAsync(Guid userId);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsAsync(Guid userId);
    }
} 