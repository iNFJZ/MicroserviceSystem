using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using Shared.EmailModels;
using System.Security.Cryptography;

namespace AuthService.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ISessionService _sessionService;
        private readonly IJwtService _jwtService;
        private readonly IEmailMessageService _emailMessageService;
        private readonly ILogger<GoogleAuthService> _logger;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public GoogleAuthService(
            IUserRepository userRepository,
            ISessionService sessionService,
            IJwtService jwtService,
            IEmailMessageService emailMessageService,
            IConfiguration config,
            HttpClient httpClient)
        {
            _userRepository = userRepository;
            _sessionService = sessionService;
            _jwtService = jwtService;
            _emailMessageService = emailMessageService;
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidGoogleTokenException("Invalid Google access token");
                }

                var content = await response.Content.ReadAsStringAsync();
                
                var googleUserInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (googleUserInfo == null || string.IsNullOrEmpty(googleUserInfo.Email))
                {
                    throw new InvalidGoogleTokenException("Invalid user info from Google");
                }


                return googleUserInfo;
            }
            catch (Exception ex) when (ex is not InvalidGoogleTokenException)
            {
                throw new InvalidGoogleTokenException("Failed to verify Google token");
            }
        }

        public async Task<string> LoginWithGoogleAsync(GoogleLoginDto dto)
        {
            var clientId = _config["GoogleAuth:ClientId"];
            var clientSecret = _config["GoogleAuth:ClientSecret"];
            
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "code", dto.Code },
                    { "client_id", clientId ?? throw new InvalidOperationException("Google ClientId is not configured") },
                    { "client_secret", clientSecret ?? throw new InvalidOperationException("Google ClientSecret is not configured") },
                    { "redirect_uri", dto.RedirectUri },
                    { "grant_type", "authorization_code" }
                })
            };
            
            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            
            if (!tokenResponse.IsSuccessStatusCode)
                throw new InvalidGoogleTokenException("Failed to exchange code for access token: " + tokenContent);

            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenContent);
            var accessToken = tokenData.GetProperty("access_token").GetString() ?? throw new InvalidGoogleTokenException("Access token is null");

            var googleUserInfo = await GetGoogleUserInfoAsync(accessToken);
        
            var existingUser = await _userRepository.GetByGoogleIdIncludeDeletedAsync(googleUserInfo.Sub);
            bool isNewUser = false;
            
            if (existingUser != null)
            {
                if (existingUser.DeletedAt.HasValue)
                {
                    throw new AuthException("Account has been deleted. Please contact support for assistance.");
                }
                
                if (existingUser.Status == UserStatus.Banned)
                {
                    throw new AuthException("Your account has been banned. Please contact support for assistance.");
                }
                
                existingUser.LoginProvider = "Google";
                if (existingUser.Status != UserStatus.Suspended)
                {
                    existingUser.Status = UserStatus.Active;
                }
                existingUser.UpdatedAt = DateTime.UtcNow;
                existingUser.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(existingUser);
            }
            else
            {
                existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email);
                
                if (existingUser != null)
                {
                    if (existingUser.DeletedAt.HasValue)
                    {
                        throw new AuthException("Account has been deleted. Please contact support for assistance.");
                    }
                    
                    if (existingUser.Status == UserStatus.Banned)
                    {
                        throw new AuthException("Your account has been banned. Please contact support for assistance.");
                    }
                    
                    existingUser.GoogleId = googleUserInfo.Sub;
                    existingUser.ProfilePicture = googleUserInfo.Picture;
                    existingUser.LoginProvider = "Google";
                    if (existingUser.Status != UserStatus.Suspended)
                    {
                        existingUser.Status = UserStatus.Active;
                    }
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    existingUser.IsVerified = true;
                    existingUser.LastLoginAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(existingUser);
                }
                else
                {
                    existingUser = new User
                    {
                        Username = googleUserInfo.Email != null && googleUserInfo.Email.Contains("@")
                            ? googleUserInfo.Email.Split('@')[0]
                            : GenerateUsernameFromGoogleInfo(googleUserInfo),
                        FullName = googleUserInfo.Name,
                        Email = googleUserInfo.Email,
                        GoogleId = googleUserInfo.Sub,
                        ProfilePicture = googleUserInfo.Picture,
                        LoginProvider = "Google",
                        CreatedAt = DateTime.UtcNow,
                        IsVerified = true,
                        Status = UserStatus.Active,
                        LastLoginAt = DateTime.UtcNow
                    };

                    await _userRepository.AddAsync(existingUser);
                    isNewUser = true;
                }
            }

            string resetToken = "";
            if (isNewUser)
            {
                resetToken = GenerateResetToken();
                var resetTokenExpiry = TimeSpan.FromHours(1);
                await _sessionService.StoreResetTokenAsync(resetToken, existingUser.Id, resetTokenExpiry);
                
                await _emailMessageService.PublishRegisterGoogleNotificationAsync(new RegisterGoogleNotificationEmailEvent
                {
                    To = existingUser.Email,
                    Username = existingUser.FullName ?? existingUser.Username,
                    Token = resetToken,
                    RegisterAt = DateTime.UtcNow,
                    Language = dto?.Language ?? "en"
                });
            }

            var token = _jwtService.GenerateToken(existingUser);
            var tokenExpiry = _jwtService.GetTokenExpirationTimeSpan(token);

            await _sessionService.StoreActiveTokenAsync(token, existingUser.Id, tokenExpiry);
            
            var sessionId = Guid.NewGuid().ToString();
            await _sessionService.CreateUserSessionAsync(existingUser.Id, sessionId, tokenExpiry);
            await _sessionService.SetUserLoginStatusAsync(existingUser.Id, true, tokenExpiry);

            return token;
        }

        private string GenerateUsernameFromGoogleInfo(GoogleUserInfo googleUserInfo)
        {
            var baseUsername = googleUserInfo.GivenName?.ToLower() ?? googleUserInfo.Name?.ToLower() ?? "user";
            
            baseUsername = System.Text.RegularExpressions.Regex.Replace(baseUsername, @"[^a-zA-Z0-9_]", "");
            
            if (string.IsNullOrEmpty(baseUsername))
                baseUsername = "user";

            var randomSuffix = new Random().Next(1000, 9999);
            return $"{baseUsername}{randomSuffix}";
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