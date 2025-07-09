using System.Text.Json;
using RabbitMQ.Client;
using Shared.EmailModels;
using System.Text;
using System;
using System.Threading.Tasks;
using UserService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UserService.Services
{
    public class EmailMessageService : IEmailMessageService
    {
        private readonly IConfiguration _config;
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _vhost;
        private readonly ILogger<EmailMessageService> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public EmailMessageService(IConfiguration config, ILogger<EmailMessageService> logger)
        {
            _config = config;
            _host = _config["RabbitMQ:HostName"] ?? "localhost";
            _port = int.Parse(_config["RabbitMQ:Port"] ?? "5672");
            _user = _config["RabbitMQ:UserName"] ?? "guest";
            _pass = _config["RabbitMQ:Password"] ?? "guest";
            _vhost = _config["RabbitMQ:VirtualHost"] ?? "/";
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _user,
                Password = _pass,
                VirtualHost = _vhost
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public Task PublishDeactivateAccountNotificationAsync(DeactivateAccountEmailEvent emailEvent)
        {
            var factory = new ConnectionFactory
            {
                HostName = _host,
                Port = _port,
                UserName = _user,
                Password = _pass,
                VirtualHost = _vhost
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "email.notifications", durable: true, exclusive: false, autoDelete: false);

            var message = JsonSerializer.Serialize(emailEvent);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "", routingKey: "email.notifications", basicProperties: null, body: body);

            return Task.CompletedTask;
        }

        public async Task PublishRestoreAccountNotificationAsync(RestoreAccountEmailEvent emailEvent)
        {
            try
            {
                var message = JsonSerializer.Serialize(emailEvent);
                var body = Encoding.UTF8.GetBytes(message);
                
                _channel.QueueDeclare("email.notifications", durable: true, exclusive: false, autoDelete: false);
                
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Headers = new Dictionary<string, object>
                {
                    { "event_type", "RestoreAccountEmailEvent" }
                };
                
                _channel.BasicPublish("", "email.notifications", properties, body);
                _logger.LogInformation($"Restore account notification published for user: {emailEvent.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish restore account notification");
            }
        }
    }
} 