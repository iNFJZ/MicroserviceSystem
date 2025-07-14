namespace Shared.EmailModels
{
    public class ChangePasswordEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime ChangeAt { get; set; }
        public string? Language { get; set; } = "en";
    }
} 