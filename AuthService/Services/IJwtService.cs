using AuthService.Models;

namespace AuthService.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user, string language = "en");
        bool ValidateToken(string token);
        Guid? GetUserIdFromToken(string token);
        DateTime? GetTokenExpiration(string token);
        TimeSpan GetTokenExpirationTimeSpan(string token);
    }
} 