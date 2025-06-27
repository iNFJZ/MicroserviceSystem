using WorkerService;
using WorkerService.Configuration;
using WorkerService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Configure options
builder.Services.Configure<RabbitMQConfig>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<FileProcessingConfig>(
    builder.Configuration.GetSection("FileProcessing"));

// Register services
builder.Services.AddSingleton<IRetryService, RetryService>();
builder.Services.AddSingleton<IFileEventProcessor, FileEventProcessor>();
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Register hosted service
builder.Services.AddHostedService<Worker>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<WorkerHealthCheck>("worker_health");

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("WorkerService starting up...");

host.Run();
