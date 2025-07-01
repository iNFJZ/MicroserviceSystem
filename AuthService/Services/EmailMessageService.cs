using AuthService.Models;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public class EmailMessageService : IEmailMessageService
    {
        private readonly IConfiguration _config;
        private readonly string _host;
        private readonly int _port;
        private readonly string _user;
        private readonly string _pass;
        private readonly string _vhost;

        public EmailMessageService(IConfiguration config)
        {
            _config = config;
            _host = _config["RabbitMQ:HostName"] ?? "localhost";
            _port = int.Parse(_config["RabbitMQ:Port"] ?? "5672");
            _user = _config["RabbitMQ:UserName"] ?? "guest";
            _pass = _config["RabbitMQ:Password"] ?? "guest";
            _vhost = _config["RabbitMQ:VirtualHost"] ?? "/";
        }

        public Task PublishRegisterNotificationAsync(RegisterNotificationEmailEvent emailEvent)
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

        public Task PublishResetPasswordNotificationAsync(ResetPasswordEmailEvent emailEvent)
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
    }
} 