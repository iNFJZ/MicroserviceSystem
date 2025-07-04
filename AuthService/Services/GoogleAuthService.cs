using AuthService.DTOs;
using AuthService.Models;
using AuthService.Repositories;
using AuthService.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

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
            ILogger<GoogleAuthService> logger,
            IConfiguration config,
            HttpClient httpClient)
        {
            _userRepository = userRepository;
            _sessionService = sessionService;
            _jwtService = jwtService;
            _emailMessageService = emailMessageService;
            _logger = logger;
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
            var googleUserInfo = await GetGoogleUserInfoAsync(dto.AccessToken);

            var existingUser = await _userRepository.GetByGoogleIdAsync(googleUserInfo.Sub);
            
            if (existingUser == null)
            {
                existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email);
                
                if (existingUser != null)
                {
                    existingUser.GoogleId = googleUserInfo.Sub;
                    existingUser.ProfilePicture = googleUserInfo.Picture;
                    existingUser.LoginProvider = "Google";
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    existingUser.IsVerified = true;
                    await _userRepository.UpdateAsync(existingUser);
                }
                else
                {
                    existingUser = new User
                    {
                        Username = GenerateUsernameFromGoogleInfo(googleUserInfo),
                        FullName = googleUserInfo.Name,
                        Email = googleUserInfo.Email,
                        GoogleId = googleUserInfo.Sub,
                        ProfilePicture = googleUserInfo.Picture,
                        LoginProvider = "Google",
                        CreatedAt = DateTime.UtcNow,
                        IsVerified = true
                    };

                    await _userRepository.AddAsync(existingUser);

                    await _emailMessageService.PublishRegisterNotificationAsync(new RegisterNotificationEmailEvent
                    {
                        To = existingUser.Email,
                        Username = existingUser.FullName ?? existingUser.Username,
                        RegisterAt = DateTime.UtcNow
                    });
                }
            }

            existingUser.LoginProvider = "Google";
            existingUser.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(existingUser);

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
    }
} 