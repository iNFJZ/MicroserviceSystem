namespace AuthService.Models
{
    public class ResetPasswordEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
        public string? ResetLink { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
} 