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
using Shared.EmailModels;

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

        public async Task<(string token, string username)> RegisterAsync(RegisterDto dto)
        {
            var sanitizedEmail = dto.Email?.Trim().ToLowerInvariant();
            var sanitizedUsername = dto.Username?.Trim();
            var sanitizedFullName = dto.FullName?.Trim();
            
            if (string.IsNullOrWhiteSpace(sanitizedEmail) || string.IsNullOrWhiteSpace(sanitizedUsername))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Email and username are required");
            
            try
            {
                var emailAddress = new System.Net.Mail.MailAddress(sanitizedEmail);
                if (emailAddress.Address != sanitizedEmail)
                    throw new AuthException("INVALID_EMAIL_FORMAT", "Invalid email format");
            }
            catch
            {
                throw new AuthException("INVALID_EMAIL_FORMAT", "Invalid email format");
            }
            
            if (sanitizedUsername.Length < 3 || sanitizedUsername.Length > 50)
                throw new AuthException("FIELD_TOO_SHORT", "Username must be between 3 and 50 characters");
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(sanitizedUsername, @"^[a-zA-Z0-9]+$"))
                throw new AuthException("INVALID_CHARACTERS", "Username can only contain letters and numbers");
            
            if (sanitizedFullName != null && !System.Text.RegularExpressions.Regex.IsMatch(sanitizedFullName, @"^[a-zA-ZÀ-ỹ\s]+$"))
                throw new AuthException("INVALID_CHARACTERS", "Full name can only contain letters, spaces, and Vietnamese characters");
            
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var phoneNumber = dto.PhoneNumber.Trim();
                if (phoneNumber.Length > 11)
                    throw new AuthException("INVALID_PHONE", "Phone number must be less than 11 characters");
                if (!System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^[0-9]{10,11}$"))
                    throw new AuthException("INVALID_PHONE", "Phone number must be 10-11 digits and contain only numbers");
            }
            
            var emailExists = await _emailVerifierService.VerifyEmailAsync(sanitizedEmail);
            if (!emailExists)
                throw new EmailNotExistsException(sanitizedEmail);

            var existingUser = await _repo.GetByEmailIncludeDeletedAsync(sanitizedEmail);
            if (existingUser != null)
            {
                if (existingUser.IsDeleted)
                    throw new AccountDeletedException();
                else
                    throw new UserAlreadyExistsException(sanitizedEmail);
            }

            var finalUsername = await GenerateUniqueUsernameAsync(sanitizedUsername);

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Password is required");
            
            if (dto.Password.Length < 6)
                throw new AuthException("WEAK_PASSWORD", "Password must be at least 6 characters");
            
            var user = new User
            {
                Username = finalUsername,
                FullName = sanitizedFullName,
                Email = sanitizedEmail,
                PhoneNumber = dto.PhoneNumber?.Trim(),
                PasswordHash = _passwordService.HashPassword(dto.Password),
                LoginProvider = "Local",
                Status = UserStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(user);
            var token = _jwtService.GenerateToken(user, dto.Language ?? "en");
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(user.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(user.Id, true, tokenExpiry);

            var verifyToken = GenerateEmailVerifyToken(user.Id, user.Email);
            var verifyLink = $"{_config["Frontend:BaseUrl"]}/auth/account-activated.html?token={verifyToken}&lang={dto.Language ?? "en"}";
            await _emailMessageService.PublishRegisterNotificationAsync(new RegisterNotificationEmailEvent
            {
                To = user.Email,
                Username = user.Username,
                RegisterAt = DateTime.UtcNow,
                VerifyLink = verifyLink,
                Language = dto.Language ?? "en"
            });
            
            return (token, finalUsername);
        }

        private async Task<string> GenerateUniqueUsernameAsync(string baseUsername)
        {
            var username = baseUsername;
            var counter = 1;
            const int maxAttempts = 100;
            while (counter <= maxAttempts)
            {
                var existingUser = await _repo.GetByUsernameAsync(username);
                if (existingUser == null)
                {
                    return username;
                }
                
                username = $"{baseUsername}{counter}";
                counter++;
            }
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            username = $"{baseUsername}{timestamp}";
            
            var finalCheck = await _repo.GetByUsernameAsync(username);
            if (finalCheck == null)
            {
                return username;
            }
            
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{baseUsername}{guid}";
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
                if (await _sessionService.IsTokenBlacklistedAsync(token))
                    return false;
                var jwt = handler.ReadJwtToken(token);
                var typeClaim = jwt.Claims.FirstOrDefault(c => c.Type == "type");
                if (typeClaim == null || typeClaim.Value != "verify") return false;
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId)) return false;
                var user = await _repo.GetByIdAsync(userId);
                if (user == null) return false;
                if (user.IsVerified) {
                    var expClaim = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
                    if (expClaim != null && long.TryParse(expClaim.Value, out long expUnix)) {
                        var expDate = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                        var ttl = expDate - DateTime.UtcNow;
                        if (ttl > TimeSpan.Zero)
                            await _sessionService.BlacklistTokenAsync(token, ttl);
                    }
                    return false;
                }
                user.IsVerified = true;
                user.Status = UserStatus.Active;
                user.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(user);
                var expClaim2 = jwt.Claims.FirstOrDefault(c => c.Type == "exp");
                if (expClaim2 != null && long.TryParse(expClaim2.Value, out long expUnix2)) {
                    var expDate2 = DateTimeOffset.FromUnixTimeSeconds(expUnix2).UtcDateTime;
                    var ttl2 = expDate2 - DateTime.UtcNow;
                    if (ttl2 > TimeSpan.Zero)
                        await _sessionService.BlacklistTokenAsync(token, ttl2);
                }
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> ResendVerificationEmailAsync(string email, string language)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Email is required");

            var user = await _repo.GetByEmailIncludeDeletedAsync(email);
            if (user == null)
                throw new UserNotFoundException(email);

            if (user.IsDeleted)
                throw new AccountDeletedException();

            if (user.IsVerified)
                throw new AuthException("EMAIL_ALREADY_VERIFIED", "Email is already verified");

            var token = GenerateEmailVerifyToken(user.Id, user.Email);
            await _sessionService.SetEmailVerifyTokenAsync(user.Id, token, TimeSpan.FromMinutes(15));

            await _emailMessageService.PublishRegisterNotificationAsync(new RegisterNotificationEmailEvent
            {
                To = user.Email,
                Username = user.FullName ?? user.Username,
                VerifyLink = $"{_config["Frontend:BaseUrl"]}/auth/account-activated.html?token={token}&lang={language ?? "en"}",
                RegisterAt = user.CreatedAt,
                Language = language ?? "en"
            });

            return true;
        }

        public async Task<string> GetEmailFromResetTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var userId = await _sessionService.GetUserIdFromResetTokenAsync(token);
            if (!userId.HasValue)
                return null;

            var user = await _repo.GetByIdAsync(userId.Value);
            return user?.Email;
        }

        public async Task<string> LoginAsync(LoginDto dto)
        {
            var sanitizedEmail = dto.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(sanitizedEmail))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Email is required");
            
            var user = await _repo.GetByEmailIncludeDeletedAsync(sanitizedEmail);
            if (user == null)
                throw new InvalidCredentialsException();
            
            if (user.IsDeleted)
                throw new AccountDeletedException();

            if (user.Status == UserStatus.Banned)
                throw new AccountBannedException();

            if (!user.IsVerified)
                throw new AccountNotVerifiedException();
            try
            {
                var emailAddress = new System.Net.Mail.MailAddress(sanitizedEmail);
                if (emailAddress.Address != sanitizedEmail)
                    throw new AuthException("INVALID_EMAIL_FORMAT", "Invalid email format");
            }
            catch
            {
                throw new AuthException("INVALID_EMAIL_FORMAT", "Invalid email format");
            }
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Password is required");
            if (dto.Password.Length < 6)
                throw new AuthException("WEAK_PASSWORD", "Password must be at least 6 characters");
            
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
            var token = _jwtService.GenerateToken(user, dto.Language ?? "en");
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);
            await _sessionService.StoreActiveTokenAsync(token, user.Id, tokenExpiry);
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(user.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(user.Id, true, tokenExpiry);
            user.LastLoginAt = DateTime.UtcNow;
            await _repo.UpdateAsync(user);
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
                if (string.IsNullOrWhiteSpace(token))
                    return false;
                
                var tokenParts = token.Split('.');
                if (tokenParts.Length != 3)
                    return false;
                
                if (token.Length < 50 || token.Length > 2000)
                    return false;

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

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto, string clientIp)
        {
            var sanitizedEmail = dto.Email?.Trim().ToLowerInvariant();
            var user = await _repo.GetByEmailIncludeDeletedAsync(sanitizedEmail);
            if (user == null)
                throw new InvalidCredentialsException();

            if (user.IsDeleted)
                throw new AccountDeletedException();

            if (user.Status == UserStatus.Banned)
                throw new AccountBannedException();

            if (!user.IsVerified)
                throw new AccountNotVerifiedException();

            var resetToken = GenerateResetToken();
            var tokenExpiry = TimeSpan.FromMinutes(_resetPasswordTokenExpiryMinutes);

            await _sessionService.StoreResetTokenAsync(resetToken, user.Id, tokenExpiry);

            var resetLink = $"{_config["Frontend:BaseUrl"]}/auth/reset-password.html?token={resetToken}";
            await _emailMessageService.PublishResetPasswordNotificationAsync(new ResetPasswordEmailEvent
            {
                To = user.Email,
                Username = user.FullName ?? user.Username,
                ResetToken = resetToken,
                ResetLink = resetLink,
                RequestedAt = DateTime.UtcNow,
                Language = dto.Language ?? "en",
                UserId = user.Id.ToString(),
                IpAddress = clientIp ?? "Unknown"
            });

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Reset token is required");
            
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new AuthException("REQUIRED_FIELD_MISSING", "New password is required");
            
            if (dto.NewPassword.Length < 6)
                throw new AuthException("WEAK_PASSWORD", "New password must be at least 6 characters");
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                throw new AuthException("WEAK_PASSWORD", "New password must contain at least one uppercase letter, one lowercase letter, and one number");
            
            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Confirm password is required");
            
            if (dto.NewPassword != dto.ConfirmPassword)
                throw new AuthException("PASSWORD_MISMATCH", "Passwords do not match");
            
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
                ChangeAt = DateTime.UtcNow,
                Language = dto.Language ?? "en"
            });

            return true;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _repo.GetByIdAsync(userId);
            if (user == null)
                throw new UserNotFoundException(userId);

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Current password is required");
            
            if (dto.CurrentPassword.Length < 6)
                throw new AuthException("WEAK_PASSWORD", "Current password must be at least 6 characters");

            if (string.IsNullOrEmpty(user.PasswordHash) || !_passwordService.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                throw new PasswordMismatchException();

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                throw new AuthException("REQUIRED_FIELD_MISSING", "New password is required");
            
            if (dto.NewPassword.Length < 6)
                throw new AuthException("WEAK_PASSWORD", "New password must be at least 6 characters");
            
            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$"))
                throw new AuthException("WEAK_PASSWORD", "New password must contain at least one uppercase letter, one lowercase letter, and one number");
            
            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
                throw new AuthException("REQUIRED_FIELD_MISSING", "Confirm password is required");
            
            if (dto.NewPassword != dto.ConfirmPassword)
                throw new AuthException("PASSWORD_MISMATCH", "Passwords do not match");

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
                ChangeAt = DateTime.UtcNow,
                Language = dto.Language ?? "en"
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
