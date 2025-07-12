using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

public class User
{
    [Key]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers, no spaces or special characters")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]*$", ErrorMessage = "Full name can only contain letters, spaces, and Vietnamese characters, no numbers or special characters")]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(200)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(500)]
    [Display(Name = "Bio")]
    public string? Bio { get; set; }

    [Display(Name = "Status")]
    public UserStatus Status { get; set; } = UserStatus.Active;

    public bool IsVerified { get; set; } = false;

    [Display(Name = "Google ID")]
    public string? GoogleId { get; set; }

    [StringLength(200000)]
    [Display(Name = "Profile Picture")]
    public string? ProfilePicture { get; set; }

    [Display(Name = "Login Provider")]
    public string LoginProvider { get; set; } = "Local";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Banned = 4
} 