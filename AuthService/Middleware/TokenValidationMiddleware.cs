using AuthService.Services;
using System.Net;

namespace AuthService.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
        {
            // Skip token validation for non-protected endpoints
            if (ShouldSkipTokenValidation(context))
            {
                await _next(context);
                return;
            }

            var token = ExtractTokenFromHeader(context);
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Token is required");
                return;
            }

            // Check if token is active in Redis
            if (!await sessionService.IsTokenActiveAsync(token))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Token is not active or has been revoked");
                return;
            }

            // Check if token is blacklisted
            if (await sessionService.IsTokenBlacklistedAsync(token))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Token has been revoked");
                return;
            }

            await _next(context);
        }

        private bool ShouldSkipTokenValidation(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // Skip validation for these endpoints
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/validate",
                "/health",
                "/swagger",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
        }

        private string? ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length);
        }
    }
} 