namespace EmailService.Models
{
    public class RegisterNotificationEmailEvent
    {
        public string To { get; set; }
        public string Username { get; set; }
        public DateTime RegisterAt { get; set; }
        public string? VerifyLink { get; set; }
    }
} 