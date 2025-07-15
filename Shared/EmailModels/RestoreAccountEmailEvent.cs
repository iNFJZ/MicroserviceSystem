namespace Shared.EmailModels;

public class RestoreAccountEmailEvent
{
    public string To { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime RestoredAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Language { get; set; } = "en";
} 