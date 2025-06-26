using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DBContext _context;
        private readonly IUserCacheService _cacheService;

        public UserRepository(DBContext context, IUserCacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            
            // Cache the new user
            await _cacheService.SetUserAsync(user);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            // Try to get from cache first
            var cachedUser = await _cacheService.GetUserByEmailAsync(email);
            if (cachedUser != null)
                return cachedUser;

            // If not in cache, get from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            // Cache the user if found
            if (user != null)
                await _cacheService.SetUserAsync(user);

            return user;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            // Try to get from cache first
            var cachedUser = await _cacheService.GetUserByIdAsync(id);
            if (cachedUser != null)
                return cachedUser;

            // If not in cache, get from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            
            // Cache the user if found
            if (user != null)
                await _cacheService.SetUserAsync(user);

            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            
            // Update cache
            await _cacheService.SetUserAsync(user);
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                // Remove from cache
                await _cacheService.DeleteUserAsync(id);
            }
        }
    }
}
