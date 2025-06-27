namespace WorkerService.Configuration;

public class FileProcessingConfig
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 10;
} 