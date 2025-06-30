using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FileService.Middleware
{
    public class AuthValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _authServiceUrl;

        public AuthValidationMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _authServiceUrl = config["AuthService:Url"] ?? "http://auth-service";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
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

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                $"{_authServiceUrl}/api/auth/validate",
                new StringContent(JsonSerializer.Serialize(new { token }), System.Text.Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token is invalid or revoked");
                return;
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AuthValidateResult>(resultJson);
            if (result is null || result.isValid == false)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token is invalid or revoked");
                return;
            }

            await _next(context);
        }

        private class AuthValidateResult
        {
            public bool isValid { get; set; }
        }
    }
} 