using AuthService.Models;
using AuthService.Repositories;
using AuthService.DTOs;
using AuthService.Exceptions;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

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
        private readonly IConfiguration _config;
        private readonly int _maxFailedLoginAttempts;
        private readonly int _accountLockMinutes;
        private readonly int _resetPasswordTokenExpiryMinutes;
        private readonly IHunterEmailVerifierService _emailVerifierService;

        public AuthService(
            IUserRepository repo, 
            ISessionService sessionService,
            IJwtService jwtService,
            IPasswordService passwordService,
            ILogger<AuthService> logger,
            IEmailMessageService emailMessageService,
            IConfiguration config,
            IHunterEmailVerifierService emailVerifierService)
        {
            _repo = repo;
            _sessionService = sessionService;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _logger = logger;
            _emailMessageService = emailMessageService;
            _config = config;
            _emailVerifierService = emailVerifierService;
            _maxFailedLoginAttempts = int.Parse(_config["AuthPolicy:MaxFailedLoginAttempts"] ?? "3");
            _accountLockMinutes = int.Parse(_config["AuthPolicy:AccountLockMinutes"] ?? "5");
            _resetPasswordTokenExpiryMinutes = int.Parse(_config["AuthPolicy:ResetPasswordTokenExpiryMinutes"] ?? "15");
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var emailExists = await _emailVerifierService.VerifyEmailAsync(dto.Email);
            if (!emailExists)
                throw new EmailNotExistsException(dto.Email);

            var existingUser = await _repo.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new UserAlreadyExistsException(dto.Email);

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = _passwordService.HashPassword(dto.Password),
                LoginProvider = "Local",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(user);
            var token = _jwtService.GenerateToken(user);
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(user.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(user.Id, true, tokenExpiry);

            var verifyToken = GenerateEmailVerifyToken(user.Id, user.Email);
            var verifyLink = $"{_config["Frontend:BaseUrl"]}/verify-email?token={verifyToken}";
            await _emailMessageService.PublishRegisterNotificationAsync(new RegisterNotificationEmailEvent
            {
                To = user.Email,
                Username = user.Username,
                RegisterAt = DateTime.UtcNow,
                VerifyLink = verifyLink
            });
            
            return token;
        }

        private string GenerateEmailVerifyToken(Guid userId, string email)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("type", "verify")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expirationTime = DateTime.UtcNow.AddHours(1);
            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: expirationTime,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwt = handler.ReadJwtToken(token);
                var typeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "type");
                if (typeClaim == null || typeClaim.Value != "verify") return false;
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId)) return false;
                var user = await _repo.GetByIdAsync(userId);
                if (user == null) return false;
                if (user.IsVerified) return true;
                user.IsVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(user);
                return true;
            }
            catch { return false; }
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null)
                throw new InvalidCredentialsException();

            var isLocked = await _sessionService.IsUserLockedAsync(user.Id);
            if (isLocked)
            {
                var lockExpiry = await _sessionService.GetUserLockExpiryAsync(user.Id);
                if (lockExpiry.HasValue && lockExpiry.Value > DateTime.UtcNow)
                {
                    var remainingMinutes = Math.Ceiling((lockExpiry.Value - DateTime.UtcNow).TotalMinutes);
                    throw new UserLockedException($"Account is locked. Please try again in {remainingMinutes} minutes.");
                }
                else
                {
                    await _sessionService.ResetFailedLoginAttemptsAsync(user.Id);
                }
            }

            if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
            {
                var failedAttempts = await _sessionService.IncrementFailedLoginAttemptsAsync(user.Id);
                
                if (failedAttempts >= _maxFailedLoginAttempts)
                {
                    var lockExpiry = DateTime.UtcNow.AddMinutes(_accountLockMinutes);
                    await _sessionService.LockUserAsync(user.Id, lockExpiry);
                    throw new UserLockedException($"Account locked due to {_maxFailedLoginAttempts} failed login attempts. Please try again in {_accountLockMinutes} minutes.");
                }
                
                throw new InvalidCredentialsException();
            }

            await _sessionService.ResetFailedLoginAttemptsAsync(user.Id);

            if (user.LoginProvider != "Local")
            {
                user.LoginProvider = "Local";
                user.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(user);
            }

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

                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue)
                    return false;

                var user = await _repo.GetByIdAsync(userId.Value);
                if (user == null)
                    return false;

                var isActiveToken = await _sessionService.IsTokenActiveAsync(token);
                if (!isActiveToken)
                    return false;

                if (!_jwtService.ValidateToken(token))
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
                return true;
            }

            var resetToken = GenerateResetToken();
            var tokenExpiry = TimeSpan.FromMinutes(_resetPasswordTokenExpiryMinutes);

            await _sessionService.StoreResetTokenAsync(resetToken, user.Id, tokenExpiry);

            await _emailMessageService.PublishResetPasswordNotificationAsync(new ResetPasswordEmailEvent
            {
                To = user.Email,
                Username = user.FullName ?? user.Username,
                ResetToken = resetToken,
                RequestedAt = DateTime.UtcNow
            });

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

            await _emailMessageService.PublishChangePasswordNotificationAsync(new ChangePasswordEmailEvent
            {
                To = user.Email,
                Username = user.FullName ?? user.Username,
                ChangeAt = DateTime.UtcNow
            });

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
                throw new UserNotFoundException(userId);

            if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new PasswordMismatchException();

            user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);

            await _sessionService.RemoveAllUserSessionsAsync(user.Id);
            await _sessionService.RemoveAllActiveTokensForUserAsync(user.Id);
            await _sessionService.SetUserLoginStatusAsync(user.Id, false);

            await _emailMessageService.PublishChangePasswordNotificationAsync(new ChangePasswordEmailEvent
            {
                To = user.Email,
                Username = user.FullName ?? user.Username,
                ChangeAt = DateTime.UtcNow
            });

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
