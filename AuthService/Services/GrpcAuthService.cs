using System.Threading.Tasks;
using Grpc.Core;
using GrpcGreeter;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Services
{
    [AllowAnonymous]
    public class GrpcAuthService : GrpcGreeter.AuthService.AuthServiceBase
    {
        public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            return Task.FromResult(new RegisterResponse
            {
                Success = true,
                Message = $"Registered user: {request.Email}"
            });
        }

        public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            return Task.FromResult(new LoginResponse
            {
                Success = true,
                Token = "mock-token-123",
                Message = "Login successful"
            });
        }

        public override Task<LogoutResponse> Logout(LogoutRequest request, ServerCallContext context)
        {
            return Task.FromResult(new LogoutResponse
            {
                Success = true,
                Message = "Logout successful"
            });
        }

        public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ValidateTokenResponse
            {
                IsValid = true,
                UserId = "mock-user-id",
                Message = "Token is valid"
            });
        }

        public override Task<GetUserSessionsResponse> GetUserSessions(GetUserSessionsRequest request, ServerCallContext context)
        {
            var resp = new GetUserSessionsResponse();
            resp.Sessions.AddRange(new[] { "session1", "session2" });
            resp.Message = "User sessions fetched";
            return Task.FromResult(resp);
        }

        public override Task<RemoveUserSessionResponse> RemoveUserSession(RemoveUserSessionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new RemoveUserSessionResponse
            {
                Success = true,
                Message = "Session removed"
            });
        }

        public override Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ForgotPasswordResponse
            {
                Success = true,
                Message = $"Password reset email sent to: {request.Email}"
            });
        }

        public override Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ResetPasswordResponse
            {
                Success = true,
                Message = "Password reset successful"
            });
        }

        public override Task<ChangePasswordResponse> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ChangePasswordResponse
            {
                Success = true,
                Message = "Password changed successfully"
            });
        }
    }
} 