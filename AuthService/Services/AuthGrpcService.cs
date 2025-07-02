using GrpcGreeter;
using Grpc.Core;
using AuthService.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public class AuthGrpcService : GrpcGreeter.AuthService.AuthServiceBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthGrpcService> _logger;

        public AuthGrpcService(IAuthService authService, ILogger<AuthGrpcService> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            try
            {
                var token = await _authService.RegisterAsync(new DTOs.RegisterDto
                {
                    Email = request.Email,
                    Password = request.Password,
                    Username = request.Email
                });
                return new RegisterResponse { Success = true, Message = "Register successful" };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Register failed");
                return new RegisterResponse { Success = false, Message = ex.Message };
            }
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            try
            {
                var token = await _authService.LoginAsync(new DTOs.LoginDto
                {
                    Email = request.Email,
                    Password = request.Password
                });
                return new LoginResponse { Success = true, Message = "Login successful", Token = token };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                return new LoginResponse { Success = false, Message = ex.Message };
            }
        }

        public override async Task<LogoutResponse> Logout(LogoutRequest request, ServerCallContext context)
        {
            try
            {
                await _authService.LogoutAsync(request.Token);
                return new LogoutResponse { Success = true, Message = "Logout successful" };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                return new LogoutResponse { Success = false, Message = ex.Message };
            }
        }

        public override async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(new DTOs.ResetPasswordDto {
                    Token = request.Token,
                    NewPassword = request.NewPassword
                });
                return new ResetPasswordResponse { Success = result, Message = result ? "Password reset successful" : "Password reset failed" };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Reset password failed");
                return new ResetPasswordResponse { Success = false, Message = ex.Message };
            }
        }
    }
} 