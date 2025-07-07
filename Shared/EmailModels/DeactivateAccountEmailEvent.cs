namespace Shared.EmailModels
{
    public class DeactivateAccountEmailEvent
    {
        public string To { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime DeactivatedAt { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
    }
} 