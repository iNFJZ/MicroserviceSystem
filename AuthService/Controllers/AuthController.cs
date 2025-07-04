using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IGoogleAuthService _googleAuth;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService auth, IGoogleAuthService googleAuth, ILogger<AuthController> logger)
        {
            _auth = auth;
            _googleAuth = googleAuth;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var token = await _auth.RegisterAsync(dto);
            return Ok(new { token });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _auth.LoginAsync(dto);
            return Ok(new { token });
        }

        [HttpPost("login/google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.AccessToken))
                return BadRequest(new { message = "Request body is required or AccessToken is required" });
            try
            {
                var token = await _googleAuth.LoginWithGoogleAsync(dto);
                return Ok(new { token });
            }
            catch (Exceptions.InvalidGoogleTokenException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var result = await _auth.LogoutAsync(token);
            
            if (result)
                return Ok(new { message = "Logged out successfully" });
            else
                return BadRequest(new { message = "Invalid token" });
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { message = "Token is required" });
            }
            var isValid = await _auth.ValidateTokenAsync(request.Token);
            return Ok(new { isValid = isValid });
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetUserSessions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized();

            var sessions = await _auth.GetUserSessionsAsync(userId);
            return Ok(new { sessions });
        }

        [Authorize]
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> RemoveUserSession(string sessionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized();

            var result = await _auth.RemoveUserSessionAsync(userId, sessionId);
            if (result)
                return Ok(new { message = "Session removed successfully" });
            else
                return NotFound(new { message = "Session not found" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var result = await _auth.ForgotPasswordAsync(dto);
            return Ok(new { message = "If the email exists, a password reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var result = await _auth.ResetPasswordAsync(dto);
            return Ok(new { message = "Password has been reset successfully." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized();

            var result = await _auth.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Password has been changed successfully." });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            var result = await _auth.VerifyEmailAsync(token);
            if (result)
                return Ok(new { message = "Email verified successfully." });
            else
                return BadRequest(new { message = "Invalid or expired token." });
        }
    }
}
