using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace UserService.Middleware;

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public TokenValidationMiddleware(RequestDelegate next, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _next = next;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkipTokenValidation(context))
        {
            await _next(context);
            return;
        }

        var token = ExtractTokenFromHeader(context);
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Token is required" });
            return;
        }

        var isValid = await ValidateTokenWithAuthService(token);
        if (!isValid)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid token" });
            return;
        }

        await _next(context);
    }

    private bool ShouldSkipTokenValidation(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        if (path?.Contains("/health") == true || 
            path?.Contains("/swagger") == true ||
            path?.Contains("/api-docs") == true)
        {
            return true;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            return true;
        }

        return false;
    }

    private string? ExtractTokenFromHeader(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length);
    }

    private async Task<bool> ValidateTokenWithAuthService(string token)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthService");
            var authServiceUrl = _configuration["AuthService:Url"] ?? "http://localhost:5001";
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{authServiceUrl}/api/auth/validate");
            request.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { token }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<ValidateTokenResponse>(content);
                return result?.IsValid ?? false;
            }
        }
        catch (Exception)
        {
        }

        return false;
    }

    private class ValidateTokenResponse
    {
        public bool IsValid { get; set; }
    }
}

public static class TokenValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenValidationMiddleware>();
    }
} 