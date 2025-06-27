using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WorkerService.Configuration;

namespace WorkerService.Services;

public class WorkerHealthCheck : IHealthCheck
{
    private readonly ILogger<WorkerHealthCheck> _logger;
    private readonly FileProcessingConfig _config;

    public WorkerHealthCheck(
        ILogger<WorkerHealthCheck> logger,
        IOptions<FileProcessingConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if configuration is valid
            if (_config.MaxRetryAttempts <= 0 || _config.RetryDelaySeconds <= 0)
            {
                _logger.LogWarning("Invalid configuration detected");
                return Task.FromResult(HealthCheckResult.Unhealthy("Invalid configuration"));
            }

            // Simulate health check logic
            var isHealthy = true; // Add your health check logic here

            if (isHealthy)
            {
                _logger.LogDebug("Health check passed");
                return Task.FromResult(HealthCheckResult.Healthy("Worker service is healthy"));
            }
            else
            {
                _logger.LogWarning("Health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy("Worker service is unhealthy"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            return Task.FromResult(HealthCheckResult.Unhealthy("Health check failed with exception", ex));
        }
    }
} 