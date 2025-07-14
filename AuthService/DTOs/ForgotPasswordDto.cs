using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [StringLength(5)]
        public string? Language { get; set; } = "en";
    }
} 