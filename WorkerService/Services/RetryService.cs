using Microsoft.Extensions.Options;
using WorkerService.Configuration;

namespace WorkerService.Services;

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly FileProcessingConfig _config;

    public RetryService(
        ILogger<RetryService> logger,
        IOptions<FileProcessingConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var attempt = 0;
        var lastException = default(Exception);

        while (attempt < _config.MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxAttempts}", 
                    operationName, attempt, _config.MaxRetryAttempts);

                return await operation();
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Operation {OperationName} failed on attempt {Attempt}/{MaxAttempts}", 
                    operationName, attempt, _config.MaxRetryAttempts);

                if (attempt < _config.MaxRetryAttempts)
                {
                    var delay = TimeSpan.FromSeconds(_config.RetryDelaySeconds * attempt);
                    _logger.LogInformation("Retrying {OperationName} in {Delay} seconds", operationName, delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }
        }

        _logger.LogError(lastException, "Operation {OperationName} failed after {MaxAttempts} attempts", 
            operationName, _config.MaxRetryAttempts);
        throw lastException!;
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, operationName);
    }
} 