using WorkerService.Models;

namespace WorkerService.Services;

public interface IRabbitMQService
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
    Task StopConsumingAsync();
    Task PublishEventAsync<T>(T eventData, string queueName) where T : class;
} 