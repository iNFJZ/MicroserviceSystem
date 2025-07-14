namespace Shared.EmailModels
{
    public class ResetPasswordEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ResetToken { get; set; } = string.Empty;
        public string? ResetLink { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public int? UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? Language { get; set; } = "en";
    }
} 