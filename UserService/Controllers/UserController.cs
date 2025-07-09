using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Services;
using UserService.Models;

namespace UserService.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
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
            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                LoginProvider = "Local",
                Status = UserStatus.Active,
                IsVerified = false
            };

            var createdUser = await _userService.CreateUserAsync(user);
            return Ok(new { success = true, data = createdUser, message = "User created successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while creating user", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all users with pagination and filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? role = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var query = new UserQueryDto
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Status = status,
                Role = role,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var (users, totalCount, totalPages) = await _userService.GetUsersAsync(query);

            return Ok(new
            {
                success = true,
                data = users,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching users", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new { success = true, data = user });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching user", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetUserByEmail(string email)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new { success = true, data = user });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching user", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new { success = true, data = user });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching user", error = ex.Message });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
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
            var result = await _userService.UpdateUserAsync(id, dto);
            if (result)
            {
                return Ok(new { success = true, message = "User updated successfully" });
            }
            else
            {
                return NotFound(new { success = false, message = "User not found" });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while updating user", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);
            if (result)
            {
                return Ok(new { success = true, message = "User has been deactivated successfully" });
            }
            else
            {
                return NotFound(new { success = false, message = "User not found or already deactivated" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while deleting user", error = ex.Message });
        }
    }

    /// <summary>
    /// Restore deleted user
    /// </summary>
    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> RestoreUser(Guid id)
    {
        try
        {
            var result = await _userService.RestoreUserAsync(id);
            if (result)
            {
                return Ok(new { success = true, message = "User has been restored successfully" });
            }
            else
            {
                return NotFound(new { success = false, message = "User not found or not deleted" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while restoring user", error = ex.Message });
        }
    }

    /// <summary>
    /// Get user statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetUserStatistics()
    {
        try
        {
            var statistics = await _userService.GetStatisticsAsync();
            return Ok(new { success = true, data = statistics });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Get deleted users
    /// </summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var query = new UserQueryDto
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                SortBy = sortBy,
                SortOrder = sortOrder,
                IncludeDeleted = true
            };

            var (users, totalCount, totalPages) = await _userService.GetDeletedUsersAsync(query);

            return Ok(new
            {
                success = true,
                data = users,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    hasNextPage = page < totalPages,
                    hasPreviousPage = page > 1
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while fetching deleted users", error = ex.Message });
        }
    }
} 