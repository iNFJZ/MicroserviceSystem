using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class LogoutDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
} 