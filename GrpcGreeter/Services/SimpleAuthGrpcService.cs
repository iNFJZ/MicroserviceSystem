using System.Threading.Tasks;
using Grpc.Core;
using GrpcGreeter;
using Microsoft.Extensions.Logging;

namespace GrpcGreeter.Services
{
    public class SimpleAuthGrpcService : GrpcGreeter.AuthService.AuthServiceBase
    {
        private readonly GrpcGreeter.AuthService.AuthServiceClient _authClient;
        private readonly ILogger<SimpleAuthGrpcService> _logger;

        public SimpleAuthGrpcService(GrpcGreeter.AuthService.AuthServiceClient authClient, ILogger<SimpleAuthGrpcService> logger)
        {
            _authClient = authClient;
            _logger = logger;
        }

        public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy Register to AuthService");
            return await _authClient.RegisterAsync(request);
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy Login to AuthService");
            return await _authClient.LoginAsync(request);
        }

        public override async Task<LogoutResponse> Logout(LogoutRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy Logout to AuthService");
            return await _authClient.LogoutAsync(request);
        }

        public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy ValidateToken to AuthService");
            return await _authClient.ValidateTokenAsync(request);
        }

        public override async Task<GetUserSessionsResponse> GetUserSessions(GetUserSessionsRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy GetUserSessions to AuthService");
            return await _authClient.GetUserSessionsAsync(request);
        }

        public override async Task<RemoveUserSessionResponse> RemoveUserSession(RemoveUserSessionRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy RemoveUserSession to AuthService");
            return await _authClient.RemoveUserSessionAsync(request);
        }

        public override async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy ForgotPassword to AuthService");
            return await _authClient.ForgotPasswordAsync(request);
        }

        public override async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy ResetPassword to AuthService");
            return await _authClient.ResetPasswordAsync(request);
        }

        public override async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Proxy ChangePassword to AuthService");
            return await _authClient.ChangePasswordAsync(request);
        }
    }
} 