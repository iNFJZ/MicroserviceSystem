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
                    errorResponse.ErrorCode = "USER_NOT_FOUND";
                    break;
                case InvalidCredentialsException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.ErrorCode = "INVALID_CREDENTIALS";
                    break;
                case UserAlreadyExistsException:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.ErrorCode = "USER_ALREADY_EXISTS";
                    break;
                case UsernameAlreadyExistsException:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.ErrorCode = "USERNAME_ALREADY_EXISTS";
                    break;
                case InvalidTokenException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.ErrorCode = "TOKEN_INVALID";
                    break;
                case AccountDeletedException:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.ErrorCode = "ACCOUNT_DELETED";
                    break;
                case AccountBannedException:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.ErrorCode = "ACCOUNT_BANNED";
                    break;
                case AccountNotVerifiedException:
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.ErrorCode = "ACCOUNT_NOT_VERIFIED";
                    break;
                case UserLockedException:
                    response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    errorResponse.ErrorCode = "ACCOUNT_LOCKED";
                    break;
                case InvalidResetTokenException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "INVALID_RESET_TOKEN";
                    break;
                case PasswordMismatchException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "PASSWORD_MISMATCH";
                    break;
                case InvalidGoogleTokenException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.ErrorCode = "GOOGLE_AUTH_FAILED";
                    break;
                case EmailNotExistsException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "EMAIL_NOT_AVAILABLE";
                    break;
                case EmailNotVerifiedException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = "EMAIL_VERIFICATION_FAILED";
                    break;
                case AuthException authEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ErrorCode = authEx.ErrorCode ?? "AUTHENTICATION_REQUIRED";
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ErrorCode = "INTERNAL_SERVER_ERROR";
                    break;
            }

            errorResponse.StatusCode = response.StatusCode;
            errorResponse.Timestamp = DateTime.UtcNow;

            var result = JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(result);
        }
    }
} 