using AuthService.DTOs;
using AuthService.Services;
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

        public AuthController(IAuthService auth, IGoogleAuthService googleAuth)
        {
            _auth = auth;
            _googleAuth = googleAuth;
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
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var token = await _auth.RegisterAsync(dto);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var token = await _auth.LoginAsync(dto);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
            catch (Exception ex)
            {
                return BadRequest(new { message = "An unexpected error occurred during Google login" });
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
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _auth.ForgotPasswordAsync(dto);
                return Ok(new { message = "If the email exists, a password reset link has been sent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _auth.ResetPasswordAsync(dto);
                return Ok(new { message = "Password has been reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
            var result = await _auth.VerifyEmailAsync(token);
            if (result)
                return Ok(new { message = "Email verified successfully." });
            else
                return BadRequest(new { message = "Invalid or expired token." });
        }

        [Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _auth.GetAllUsersAsync();
            var result = users.Select(u => new {
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.DateOfBirth,
                u.Address,
                u.Bio,
                u.Status,
                u.LoginProvider,
                u.IsVerified,
                u.CreatedAt,
                u.LastLoginAt,
                u.DeletedAt,
                IsDeleted = u.IsDeleted
            });
            return Ok(result);
        }

        [Authorize]
        [HttpPatch("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _auth.UpdateUserAsync(id, dto);
                if (result)
                    return Ok(new { message = "User updated successfully" });
                else
                    return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var result = await _auth.DeleteUserAsync(id);
                if (result)
                    return Ok(new { message = "User has been deactivated successfully" });
                else
                    return NotFound(new { message = "User not found or already deactivated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
