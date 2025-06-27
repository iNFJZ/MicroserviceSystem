using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using FileService.Models;
using Microsoft.Extensions.Options;
using System.Threading;

namespace FileService.Services
{
    public class FileEventConsumer : IFileEventConsumer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<FileEventConsumer> _logger;

        private const string FileUploadQueue = "file.upload";
        private const string FileDownloadQueue = "file.download";
        private const string FileDeleteQueue = "file.delete";

        public FileEventConsumer(IOptions<RabbitMQOptions> options, ILogger<FileEventConsumer> logger)
        {
            _logger = logger;
            var config = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = config.HostName,
                Port = config.Port,
                UserName = config.UserName,
                Password = config.Password,
                VirtualHost = config.VirtualHost
            };

            int retryCount = 0;
            const int maxRetries = 10;
            const int delayMs = 3000;
            while (true)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount > maxRetries)
                    {
                        _logger.LogError(ex, $"Failed to connect to RabbitMQ after {maxRetries} attempts.");
                        throw;
                    }
                    _logger.LogWarning(ex, $"RabbitMQ not ready, retrying in {delayMs / 1000} seconds... (Attempt {retryCount}/{maxRetries})");
                    Thread.Sleep(delayMs);
                }
            }

            _channel.QueueDeclare(FileUploadQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(FileDownloadQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(FileDeleteQueue, durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("FileEventConsumer initialized");
        }

        public void StartConsuming()
        {
            var uploadConsumer = new EventingBasicConsumer(_channel);
            uploadConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    var uploadEvent = JsonSerializer.Deserialize<FileUploadEvent>(message);
                    _logger.LogInformation("File upload event received: {FileName} ({FileSize} bytes) at {UploadTime}", 
                        uploadEvent?.FileName, uploadEvent?.FileSize, uploadEvent?.UploadTime);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing upload event: {Message}", message);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            var downloadConsumer = new EventingBasicConsumer(_channel);
            downloadConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    var downloadEvent = JsonSerializer.Deserialize<FileDownloadEvent>(message);
                    _logger.LogInformation("File download event received: {FileName} at {DownloadTime}", 
                        downloadEvent?.FileName, downloadEvent?.DownloadTime);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing download event: {Message}", message);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            var deleteConsumer = new EventingBasicConsumer(_channel);
            deleteConsumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    var deleteEvent = JsonSerializer.Deserialize<FileDeleteEvent>(message);
                    _logger.LogInformation("File delete event received: {FileName} at {DeleteTime}", 
                        deleteEvent?.FileName, deleteEvent?.DeleteTime);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing delete event: {Message}", message);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            var uploadTag = _channel.BasicConsume(queue: FileUploadQueue, autoAck: false, consumer: uploadConsumer);
            var downloadTag = _channel.BasicConsume(queue: FileDownloadQueue, autoAck: false, consumer: downloadConsumer);
            var deleteTag = _channel.BasicConsume(queue: FileDeleteQueue, autoAck: false, consumer: deleteConsumer);

            _logger.LogInformation("Started consuming file events from RabbitMQ");
        }

        public void StopConsuming()
        {
            _logger.LogInformation("Stopped consuming file events");
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
} 