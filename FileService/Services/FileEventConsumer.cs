using Microsoft.Extensions.Logging;

namespace FileService.Services
{
    public class FileEventConsumer : IFileEventConsumer, IDisposable
    {
        private readonly ILogger<FileEventConsumer> _logger;

        public FileEventConsumer(ILogger<FileEventConsumer> logger)
        {
            _logger = logger;
            _logger.LogInformation("FileEventConsumer initialized (RabbitMQ logic removed)");
        }

        public void StartConsuming()
        {
            // No-op: RabbitMQ logic removed
        }

        public void StopConsuming()
        {
            // No-op: RabbitMQ logic removed
        }

        public void Dispose()
        {
            // No-op: RabbitMQ logic removed
        }
    }
} 