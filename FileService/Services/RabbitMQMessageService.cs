using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using FileService.Models;
using Microsoft.Extensions.Options;

namespace FileService.Services
{
    public class RabbitMQMessageService : IMessageService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQMessageService> _logger;

        private const string FileUploadQueue = "file.upload";
        private const string FileDownloadQueue = "file.download";
        private const string FileDeleteQueue = "file.delete";

        public RabbitMQMessageService(IOptions<RabbitMQOptions> options, ILogger<RabbitMQMessageService> logger)
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

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare queues
            _channel.QueueDeclare(FileUploadQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(FileDownloadQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(FileDeleteQueue, durable: true, exclusive: false, autoDelete: false);

            _logger.LogInformation("RabbitMQ connection established");
        }

        public Task PublishFileUploadEventAsync(FileUploadEvent fileEvent)
        {
            return PublishEventAsync(FileUploadQueue, fileEvent);
        }

        public Task PublishFileDownloadEventAsync(FileDownloadEvent fileEvent)
        {
            return PublishEventAsync(FileDownloadQueue, fileEvent);
        }

        public Task PublishFileDeleteEventAsync(FileDeleteEvent fileEvent)
        {
            return PublishEventAsync(FileDeleteQueue, fileEvent);
        }

        private Task PublishEventAsync<T>(string queueName, T eventData)
        {
            try
            {
                var message = JsonSerializer.Serialize(eventData);
                var body = Encoding.UTF8.GetBytes(message);

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: null,
                    body: body);

                _logger.LogInformation("Message published to queue {QueueName}: {Message}", queueName, message);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to queue {QueueName}", queueName);
                throw;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
} 