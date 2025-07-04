using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class GoogleLoginDto
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;
    }
} 