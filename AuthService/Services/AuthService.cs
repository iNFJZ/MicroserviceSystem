using AuthService.Models;
using AuthService.Repositories;
using AuthService.DTOs;
using AuthService.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly ISessionService _sessionService;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailMessageService _emailMessageService;

        public AuthService(
            IUserRepository repo, 
            ISessionService sessionService,
            IJwtService jwtService,
            IPasswordService passwordService,
            ILogger<AuthService> logger,
            IEmailMessageService emailMessageService)
        {
            _repo = repo;
            _sessionService = sessionService;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _logger = logger;
            _emailMessageService = emailMessageService;
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
            
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(user.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(user.Id, true, tokenExpiry);

            await _emailMessageService.PublishRegisterNotificationAsync(new RegisterNotificationEmailEvent
            {
                To = user.Email,
                Username = user.Username,
                RegisterAt = DateTime.UtcNow
            });

            return token;
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            var token = _jwtService.GenerateToken(user);
            
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            
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

                var user = await _repo.GetByIdAsync(userId.Value);

                await _sessionService.RemoveActiveTokenAsync(token);
                
                var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
                await _sessionService.BlacklistTokenAsync(token, tokenExpiry);
                
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
                if (await _sessionService.IsTokenBlacklistedAsync(token))
                    return false;

                if (!await _sessionService.IsTokenActiveAsync(token))
                    return false;

                if (!_jwtService.ValidateToken(token))
                    return false;

                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue)
                    return false;

                var user = await _repo.GetByIdAsync(userId.Value);
                if (user == null)
                    return false;

                var storedUserId = await _sessionService.GetUserIdFromActiveTokenAsync(token);
                if (!storedUserId.HasValue || storedUserId.Value != userId.Value)
                    return false;

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

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogInformation("Password reset requested for email: {Email}", dto.Email);
                return true;
            }

            var resetToken = GenerateResetToken();
            var tokenExpiry = TimeSpan.FromMinutes(15);

            await _sessionService.StoreResetTokenAsync(resetToken, user.Id, tokenExpiry);

            await _emailMessageService.PublishResetPasswordNotificationAsync(new ResetPasswordEmailEvent
            {
                To = user.Email,
                Username = user.Username,
                ResetToken = resetToken,
                RequestedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Password reset email sent to: {Email}", dto.Email);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var userId = await _sessionService.GetUserIdFromResetTokenAsync(dto.Token);
            if (!userId.HasValue)
                throw new InvalidResetTokenException();

            var user = await _repo.GetByIdAsync(userId.Value);
            if (user == null)
                throw new UserNotFoundException(userId.Value);

            user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);

            await _sessionService.RemoveResetTokenAsync(dto.Token);

            await _sessionService.RemoveAllUserSessionsAsync(user.Id);
            await _sessionService.RemoveAllActiveTokensForUserAsync(user.Id);
            await _sessionService.SetUserLoginStatusAsync(user.Id, false);

            _logger.LogInformation("Password reset successful for user: {UserId}", user.Id);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
                throw new UserNotFoundException(userId);

            if (!_passwordService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new PasswordMismatchException();

            user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);

            await _sessionService.RemoveAllUserSessionsAsync(user.Id);
            await _sessionService.RemoveAllActiveTokensForUserAsync(user.Id);
            await _sessionService.SetUserLoginStatusAsync(user.Id, false);

            _logger.LogInformation("Password changed successfully for user: {UserId}", user.Id);
            return true;
        }

        private string GenerateResetToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
