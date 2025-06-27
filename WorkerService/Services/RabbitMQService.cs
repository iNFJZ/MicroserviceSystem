using WorkerService.Models;
using WorkerService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace WorkerService.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly RabbitMQConfig _config;
    private readonly IFileEventProcessor _fileEventProcessor;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    // Queue names
    private const string FileUploadQueue = "file.upload";
    private const string FileDownloadQueue = "file.download";
    private const string FileDeleteQueue = "file.delete";

    public RabbitMQService(
        ILogger<RabbitMQService> logger,
        IOptions<RabbitMQConfig> config,
        IFileEventProcessor fileEventProcessor)
    {
        _logger = logger;
        _config = config.Value;
        _fileEventProcessor = fileEventProcessor;
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InitializeConnectionAsync();
            await SetupQueuesAsync();
            await StartConsumersAsync(cancellationToken);
            
            _logger.LogInformation("RabbitMQ service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ service");
            throw;
        }
    }

    public async Task StopConsumingAsync()
    {
        try
        {
            if (_channel?.IsOpen == true)
            {
                _channel.Close();
            }
            
            if (_connection?.IsOpen == true)
            {
                _connection.Close();
            }
            
            _logger.LogInformation("RabbitMQ service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping RabbitMQ service");
        }
        
        await Task.CompletedTask;
    }

    public async Task PublishEventAsync<T>(T eventData, string queueName) where T : class
    {
        try
        {
            if (_channel?.IsOpen != true)
            {
                await InitializeConnectionAsync();
            }

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            _channel!.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Published event to queue {QueueName}: {Message}", queueName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to queue {QueueName}", queueName);
            throw;
        }
        
        await Task.CompletedTask;
    }

    private async Task InitializeConnectionAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.UserName,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("Connected to RabbitMQ at {HostName}:{Port}", _config.HostName, _config.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
        
        await Task.CompletedTask;
    }

    private async Task SetupQueuesAsync()
    {
        try
        {
            // Declare queues
            _channel!.QueueDeclare(FileUploadQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(FileDownloadQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(FileDeleteQueue, durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("Queues declared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to declare queues");
            throw;
        }
        
        await Task.CompletedTask;
    }

    private async Task StartConsumersAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Start consumer for file upload events
            var uploadConsumer = new AsyncEventingBasicConsumer(_channel);
            uploadConsumer.Received += async (model, ea) => await HandleFileUploadEventAsync(ea);
            _channel!.BasicConsume(queue: FileUploadQueue, autoAck: false, consumer: uploadConsumer);

            // Start consumer for file download events
            var downloadConsumer = new AsyncEventingBasicConsumer(_channel);
            downloadConsumer.Received += async (model, ea) => await HandleFileDownloadEventAsync(ea);
            _channel.BasicConsume(queue: FileDownloadQueue, autoAck: false, consumer: downloadConsumer);

            // Start consumer for file delete events
            var deleteConsumer = new AsyncEventingBasicConsumer(_channel);
            deleteConsumer.Received += async (model, ea) => await HandleFileDeleteEventAsync(ea);
            _channel.BasicConsume(queue: FileDeleteQueue, autoAck: false, consumer: deleteConsumer);

            _logger.LogInformation("Consumers started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consumers");
            throw;
        }
        
        await Task.CompletedTask;
    }

    private async Task HandleFileUploadEventAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var fileEvent = JsonSerializer.Deserialize<FileUploadEvent>(message);

            if (fileEvent != null)
            {
                await _fileEventProcessor.ProcessFileUploadEventAsync(fileEvent);
                _channel!.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Successfully processed file upload event for {FileName}", fileEvent.FileName);
            }
            else
            {
                _channel!.BasicNack(ea.DeliveryTag, false, true);
                _logger.LogWarning("Failed to deserialize file upload event");
            }
        }
        catch (Exception ex)
        {
            _channel!.BasicNack(ea.DeliveryTag, false, true);
            _logger.LogError(ex, "Error processing file upload event");
        }
    }

    private async Task HandleFileDownloadEventAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var fileEvent = JsonSerializer.Deserialize<FileDownloadEvent>(message);

            if (fileEvent != null)
            {
                await _fileEventProcessor.ProcessFileDownloadEventAsync(fileEvent);
                _channel!.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Successfully processed file download event for {FileName}", fileEvent.FileName);
            }
            else
            {
                _channel!.BasicNack(ea.DeliveryTag, false, true);
                _logger.LogWarning("Failed to deserialize file download event");
            }
        }
        catch (Exception ex)
        {
            _channel!.BasicNack(ea.DeliveryTag, false, true);
            _logger.LogError(ex, "Error processing file download event");
        }
    }

    private async Task HandleFileDeleteEventAsync(BasicDeliverEventArgs ea)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var fileEvent = JsonSerializer.Deserialize<FileDeleteEvent>(message);

            if (fileEvent != null)
            {
                await _fileEventProcessor.ProcessFileDeleteEventAsync(fileEvent);
                _channel!.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Successfully processed file delete event for {FileName}", fileEvent.FileName);
            }
            else
            {
                _channel!.BasicNack(ea.DeliveryTag, false, true);
                _logger.LogWarning("Failed to deserialize file delete event");
            }
        }
        catch (Exception ex)
        {
            _channel!.BasicNack(ea.DeliveryTag, false, true);
            _logger.LogError(ex, "Error processing file delete event");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
} 