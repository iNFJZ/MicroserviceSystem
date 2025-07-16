using System.ComponentModel.DataAnnotations;

namespace UserService.DTOs;

public class CreateUserDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers, no spaces or special characters")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]*$", ErrorMessage = "Full name can only contain letters, spaces, and Vietnamese characters")]
    public string? FullName { get; set; }

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200000)]
    public string? ProfilePicture { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
} 