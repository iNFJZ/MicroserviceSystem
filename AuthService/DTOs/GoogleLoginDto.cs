using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class GoogleLoginDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        [Required]
        public string RedirectUri { get; set; } = string.Empty;
        public string? Language { get; set; } = "en";
    }
} 