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
        private readonly ISessionService _sessionService;

        public UserRepository(DBContext context, IUserCacheService cacheService, ISessionService sessionService)
        {
            _context = context;
            _cacheService = cacheService;
            _sessionService = sessionService;
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
            {
                var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (userInDb == null)
                {
                    await _cacheService.DeleteUserByEmailAsync(email);
                    return null;
                }
                return userInDb; 
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
                await _cacheService.SetUserAsync(user);

            return user;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                await _cacheService.SetUserAsync(user);
                return user;
            }
            else
            {
                await _cacheService.DeleteUserAsync(id);
                return null;
            }
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
            if (user != null)
            {
                await _cacheService.SetUserAsync(user);
                return user;
            }
            return null;
        }

        public async Task UpdateAsync(User user)
        {
            var existingUser = await _context.Users.FindAsync(user.Id);
            if (existingUser != null)
            {
                _context.Entry(existingUser).CurrentValues.SetValues(user);
                await _context.SaveChangesAsync();
                await _cacheService.SetUserAsync(existingUser);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await _cacheService.DeleteUserAsync(id);
                await _sessionService.RemoveAllUserSessionsAsync(id);
            }
        }
    }
}
