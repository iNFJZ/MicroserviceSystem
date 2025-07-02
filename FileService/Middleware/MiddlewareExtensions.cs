using Microsoft.AspNetCore.Builder;

namespace FileService.Middleware
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenValidationMiddleware>();
        }
    }
} 