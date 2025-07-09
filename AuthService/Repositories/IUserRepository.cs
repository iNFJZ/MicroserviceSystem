using AuthService.Models;

namespace AuthService.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByEmailIncludeDeletedAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByGoogleIdAsync(string googleId);
        Task<User?> GetByGoogleIdIncludeDeletedAsync(string googleId);
        Task<User?> GetByUsernameAsync(string username);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}
