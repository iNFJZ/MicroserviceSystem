namespace AuthService.Models
{
    public class RegisterNotificationEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime RegisterAt { get; set; }
        public string? VerifyLink { get; set; }
    }
} 