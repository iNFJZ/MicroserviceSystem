using System.Threading.Tasks;
using Grpc.Core;
using GrpcGreeter;

namespace GrpcGreeter.Services
{
    public class SimpleAuthGrpcService : GrpcGreeter.AuthService.AuthServiceBase
    {
        private readonly GrpcGreeter.AuthService.AuthServiceClient _authClient;

        public SimpleAuthGrpcService(GrpcGreeter.AuthService.AuthServiceClient authClient)
        {
            _authClient = authClient;
        }

        public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            return await _authClient.RegisterAsync(request);
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            return await _authClient.LoginAsync(request);
        }

        public override async Task<LogoutResponse> Logout(LogoutRequest request, ServerCallContext context)
        {
            return await _authClient.LogoutAsync(request);
        }

        public override async Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            return await _authClient.ValidateTokenAsync(request);
        }

        public override async Task<GetUserSessionsResponse> GetUserSessions(GetUserSessionsRequest request, ServerCallContext context)
        {
            return await _authClient.GetUserSessionsAsync(request);
        }

        public override async Task<RemoveUserSessionResponse> RemoveUserSession(RemoveUserSessionRequest request, ServerCallContext context)
        {
            return await _authClient.RemoveUserSessionAsync(request);
        }

        public override async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request, ServerCallContext context)
        {
            return await _authClient.ForgotPasswordAsync(request);
        }

        public override async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
        {
            return await _authClient.ResetPasswordAsync(request);
        }

        public override async Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
        {
            return await _authClient.ChangePasswordAsync(request);
        }
    }
} 