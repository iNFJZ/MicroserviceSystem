using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Services
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<string> LoginAsync(LoginDto dto);
        Task<bool> LogoutAsync(string token);
        Task<bool> ValidateTokenAsync(string token);
        Task<IEnumerable<string>> GetUserSessionsAsync(Guid userId);
        Task<bool> RemoveUserSessionAsync(Guid userId, string sessionId);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto dto, string clientIp);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<bool> VerifyEmailAsync(string token);
        Task<bool> ResendVerificationEmailAsync(string email);
        Task<string> GetEmailFromResetTokenAsync(string token);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(Guid userId, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(Guid userId);
    }
}
