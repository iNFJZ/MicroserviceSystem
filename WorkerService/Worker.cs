namespace WorkerService;
using WorkerService.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client.Events;

public class Worker : BackgroundService, IDisposable
{
    private readonly ILogger<Worker> _logger;
    private IConnection _connection;
    private readonly IModel _channel;
    private const string FileUploadQueue = "file.upload";
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        var factory = new ConnectionFactory()
        {
            HostName = "rabbitmq",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(FileUploadQueue, durable: true, exclusive: false, autoDelete: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var fileEvent = JsonSerializer.Deserialize<FileUploadEvent>(message);
            _logger.LogInformation("Received message: {message}", message);

            //Logic
            System.Console.WriteLine("Testing");
            await Task.CompletedTask;
        };

        _channel.BasicConsume(queue: FileUploadQueue, autoAck: true, consumer: consumer);

        await Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
        base.Dispose();
    }
}
