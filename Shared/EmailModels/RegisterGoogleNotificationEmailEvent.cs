namespace Shared.EmailModels
{
    public class RegisterGoogleNotificationEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime RegisterAt { get; set; }
    } 
} 