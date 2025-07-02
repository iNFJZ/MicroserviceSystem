using AuthService.Exceptions;
using AuthService.DTOs;
using System.Net;
using System.Text.Json;

namespace AuthService.Middleware
{
    public class GlobalExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.ContentType != null && context.Request.ContentType.StartsWith("application/grpc"))
            {
                await _next(context);
                return;
            }
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case UserNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = exception.Message;
                    break;
                case InvalidCredentialsException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = exception.Message;
                    break;
                case UserAlreadyExistsException:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.Message = exception.Message;
                    break;
                case InvalidTokenException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = exception.Message;
                    break;
                case AuthException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = exception.Message;
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "An unexpected error occurred.";
                    _logger.LogError(exception, "Unhandled exception occurred");
                    break;
            }

            errorResponse.StatusCode = response.StatusCode;
            errorResponse.Timestamp = DateTime.UtcNow;

            var result = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(result);
        }
    }
} 