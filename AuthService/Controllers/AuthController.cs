using AuthService.DTOs;
using AuthService.Services;
using AuthService.Extensions;
using Microsoft.AspNetCore.Authorization;               
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly IGoogleAuthService _googleAuth;
        private readonly IConfiguration _config;

        public AuthController(IAuthService auth, IGoogleAuthService googleAuth, IConfiguration config)
        {
            _auth = auth;
            _googleAuth = googleAuth;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            var (token, username) = await _auth.RegisterAsync(dto);
            return Ok(new { 
                success = true, 
                message = "Registration successful",
                token,
                username,
                redirectUrl = $"{_config["Frontend:BaseUrl"]}/auth/account-activated.html"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            var token = await _auth.LoginAsync(dto);
            return Ok(new { 
                success = true, 
                message = "Login successful",
                token,
                redirectUrl = $"{_config["Frontend:BaseUrl"]}/admin/user-list.html"
            });
        }

        [HttpPost("login/google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginDto dto)
        {
            
            if (dto == null || string.IsNullOrEmpty(dto.Code) || string.IsNullOrEmpty(dto.RedirectUri))
            {
                return BadRequest(new { message = "Code and RedirectUri are required" });
            }
            
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            var clientIp = HttpContext.GetClientIpAddress();
            var result = await _auth.ForgotPasswordAsync(dto, clientIp);
            return Ok(new { 
                success = true, 
                message = "If the email exists, a password reset link has been sent to your email address.",
                redirectUrl = $"{_config["Frontend:BaseUrl"]}/auth/login.html?message=reset_link_sent"
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            try
            {
                var result = await _auth.ResetPasswordAsync(dto);
                return Ok(new { 
                    success = true, 
                    message = "Password has been reset successfully.",
                    redirectUrl = $"{_config["Frontend:BaseUrl"]}/auth/login.html?message=password_reset_success"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
                return Unauthorized();

            try
            {
                var result = await _auth.ChangePasswordAsync(userId, dto);
                return Ok(new { message = "Password has been changed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }

            var result = await _auth.VerifyEmailAsync(token);
            if (result)
                return Ok(new { success = true, message = "Email verified successfully" });
            else
                return BadRequest(new { success = false, message = "Invalid or expired token" });
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            try
            {
                var result = await _auth.ResendVerificationEmailAsync(dto.Email, dto.Language ?? "en");
                return Ok(new { 
                    success = true, 
                    message = "Verification email has been resent successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("validate-reset-token")]
        public async Task<IActionResult> ValidateResetToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }

            try
            {
                var email = await _auth.GetEmailFromResetTokenAsync(token);
                if (!string.IsNullOrEmpty(email))
                {
                    return Ok(new { success = true, email });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid or expired token" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


    }
}
