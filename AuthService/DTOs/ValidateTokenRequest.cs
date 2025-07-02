using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs
{
    public class ValidateTokenRequest
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
} 