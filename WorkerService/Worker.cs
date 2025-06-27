using WorkerService.Services;
using Microsoft.Extensions.Logging;

namespace WorkerService;
using WorkerService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IRabbitMQService _rabbitMQService;

    public Worker(ILogger<Worker> logger, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _rabbitMQService = rabbitMQService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service starting...");

        try
        {
            await _rabbitMQService.StartConsumingAsync(stoppingToken);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                _logger.LogDebug("Worker service is running...");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker service is stopping due to cancellation request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in worker service");
            throw;
        }
        finally
        {
            await _rabbitMQService.StopConsumingAsync();
            _logger.LogInformation("Worker service stopped");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker service is stopping...");
        await _rabbitMQService.StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }
}
