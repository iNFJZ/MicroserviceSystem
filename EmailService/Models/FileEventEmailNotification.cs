namespace EmailService.Models
{
    public class FileEventEmailNotification
    {
        public string To { get; set; }
        public string Username { get; set; }
        public string FileName { get; set; }
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public string? Language { get; set; } = "en";
    }
} 