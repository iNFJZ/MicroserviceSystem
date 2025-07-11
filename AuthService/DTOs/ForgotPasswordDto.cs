using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
} 