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
            await _cacheService.SetUserAsync(user);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var cachedUser = await _cacheService.GetUserByEmailAsync(email);
            if (cachedUser != null)
                return cachedUser;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
                await _cacheService.SetUserAsync(user);

            return user;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var cachedUser = await _cacheService.GetUserByIdAsync(id);
            if (cachedUser != null)
                return cachedUser;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
                await _cacheService.SetUserAsync(user);

            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            await _cacheService.SetUserAsync(user);
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await _cacheService.DeleteUserAsync(id);
            }
        }
    }
}
