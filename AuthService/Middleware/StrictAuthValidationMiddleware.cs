using Microsoft.AspNetCore.Http;
using System.Text.Json;
using AuthService.Services;
using System.Linq;

namespace AuthService.Middleware
{
    public class StrictAuthValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public StrictAuthValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private bool ShouldSkipTokenValidation(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            var skipPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/register",
                "/api/auth/validate",
                "/api/auth/forgot-password",
                "/api/auth/reset-password",
                "/api/auth/verify-email",
                "/health",
                "/swagger",
                "/favicon.ico"
            };
            return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
        }

        public async Task InvokeAsync(HttpContext context, IAuthService authService)
        {
            if (context.Request.ContentType?.StartsWith("application/grpc") == true)
            {
                await _next(context);
                return;
            }
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

            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing or invalid Authorization header");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length);

            var isValid = await authService.ValidateTokenAsync(token);
            if (!isValid)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token is invalid or user does not exist");
                return;
            }

            await _next(context);
        }
    }
} 