namespace FileService.Services
{
    public interface IFileEventConsumer
    {
        void StartConsuming();
        void StopConsuming();
    }
} 