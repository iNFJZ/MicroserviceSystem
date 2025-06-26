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
    }
}
