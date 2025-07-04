using System.ComponentModel.DataAnnotations;

namespace AuthService.Models;
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

    [StringLength(100)]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]*$", ErrorMessage = "Full name can only contain letters, spaces, and Vietnamese characters, no numbers or special characters")]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? PasswordHash { get; set; }
    
    [Display(Name = "Google ID")]
    public string? GoogleId { get; set; }
    
    [Display(Name = "Profile Picture")]
    public string? ProfilePicture { get; set; }
    
    [Display(Name = "Login Provider")]
    public string LoginProvider { get; set; } = "Local";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsVerified { get; set; } = false;
}
