using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Username can only contain letters and numbers, no spaces or special characters")]
        public string Username { get; set; } = string.Empty;
        
        [StringLength(100)]
        [RegularExpression(@"^[a-zA-ZÀ-ỹ\s]+$", ErrorMessage = "Full name can only contain letters, spaces, and Vietnamese characters, no numbers or special characters")]
        public string? FullName { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$", 
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number")]
        public string Password { get; set; } = string.Empty;

        [StringLength(5)]
        public string? Language { get; set; } = "en";
    }
}
