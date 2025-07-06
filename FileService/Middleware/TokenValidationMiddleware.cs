using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _authServiceUrl;

        public TokenValidationMiddleware(
            RequestDelegate next, 
            ILogger<TokenValidationMiddleware> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _authServiceUrl = configuration["AuthService:Url"] ?? "http://localhost:5001";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (ShouldSkipTokenValidation(context))
            {
                await _next(context);
                return;
            }

            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }

            var authorizeAttribute = endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
            if (authorizeAttribute == null)
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing or invalid Authorization header");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length);

            try
            {
                var isValid = await ValidateTokenWithAuthServiceAsync(token);
                if (!isValid)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token is invalid or has been revoked");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token with AuthService");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Error validating token");
                return;
            }

            await _next(context);
        }

        private async Task<bool> ValidateTokenWithAuthServiceAsync(string token)
        {
            try
            {
                var request = new { Token = token };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Validating token with AuthService at: {AuthServiceUrl}", _authServiceUrl);
                using var httpClient = _httpClientFactory.CreateClient("AuthService");
                var response = await httpClient.PostAsync($"{_authServiceUrl}/api/auth/validate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("AuthService response: {Response}", responseContent);
                    var result = JsonSerializer.Deserialize<ValidateTokenResponse>(responseContent);
                    var isValid = result?.IsValid ?? false;
                    _logger.LogInformation("Token validation result: {IsValid}", isValid);
                    return isValid;
                }

                _logger.LogWarning("AuthService returned non-success status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate token with AuthService");
                return false;
            }
        }

        private bool ShouldSkipTokenValidation(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            if (string.IsNullOrEmpty(path)) return false;
            return path.Contains("/swagger") || path.Contains("/favicon.ico") || path.Contains("/health");
        }

        private class ValidateTokenResponse
        {
            [JsonPropertyName("isValid")]
            public bool IsValid { get; set; }
        }
    }
} 