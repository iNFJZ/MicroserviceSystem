namespace AuthService.Models
{
    public class ChangePasswordEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime ChangeAt { get; set; }
    }
} 