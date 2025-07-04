using AuthService.DTOs;

namespace AuthService.Services
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken);
        Task<string> LoginWithGoogleAsync(GoogleLoginDto dto);
    }
} 