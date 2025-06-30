using AuthService.Models;
using AuthService.Repositories;
using AuthService.DTOs;
using AuthService.Exceptions;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly ISessionService _sessionService;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;

        public AuthService(
            IUserRepository repo, 
            ISessionService sessionService,
            IJwtService jwtService,
            IPasswordService passwordService)
        {
            _repo = repo;
            _sessionService = sessionService;
            _jwtService = jwtService;
            _passwordService = passwordService;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _repo.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new UserAlreadyExistsException(dto.Email);

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = _passwordService.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(user);
            var token = _jwtService.GenerateToken(user);
            
            // Get token expiration time
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            
            // Store token in Redis with expiration
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            
            // Create session for new user
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(user.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(user.Id, true, tokenExpiry);
            
            return token;
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            var token = _jwtService.GenerateToken(user);
            
            // Get token expiration time
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            
            // Store token in Redis with expiration
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            
            // Create session for logged in user
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(user.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(user.Id, true, tokenExpiry);
            
            return token;
        }

        public async Task<bool> LogoutAsync(string token)
        {
            try
            {
                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue)
                    return false;

                // Remove token from active tokens
                await _sessionService.RemoveActiveTokenAsync(token);
                
                // Blacklist the token
                var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
                await _sessionService.BlacklistTokenAsync(token, tokenExpiry);
                
                // Remove user login status
                await _sessionService.SetUserLoginStatusAsync(userId.Value, false);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                // Check if token is blacklisted
                if (await _sessionService.IsTokenBlacklistedAsync(token))
                    return false;

                // Check if token exists in Redis (active token)
                if (!await _sessionService.IsTokenActiveAsync(token))
                    return false;

                if (!_jwtService.ValidateToken(token))
                    return false;

                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue)
                    return false;

                // Verify the token belongs to the correct user
                var storedUserId = await _sessionService.GetUserIdFromActiveTokenAsync(token);
                if (!storedUserId.HasValue || storedUserId.Value != userId.Value)
                    return false;

                // Check if user is still logged in
                if (!await _sessionService.IsUserLoggedInAsync(userId.Value))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserSessionsAsync(Guid userId)
        {
            return await _sessionService.GetUserSessionsAsync(userId);
        }

        public async Task<bool> RemoveUserSessionAsync(Guid userId, string sessionId)
        {
            return await _sessionService.RemoveUserSessionAsync(userId, sessionId);
        }
    }
}
