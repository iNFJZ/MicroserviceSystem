using WorkerService.Models;
using Microsoft.Extensions.Logging;

namespace WorkerService.Services;

public class FileEventProcessor : IFileEventProcessor
{
    private readonly ILogger<FileEventProcessor> _logger;
    private readonly IRetryService _retryService;

    public FileEventProcessor(
        ILogger<FileEventProcessor> logger,
        IRetryService retryService)
    {
        _logger = logger;
        _retryService = retryService;
    }

    public async Task ProcessFileUploadEventAsync(FileUploadEvent fileEvent)
    {
        await _retryService.ExecuteWithRetryAsync(async () =>
        {
            try
            {
                _logger.LogInformation("Processing file upload event for file: {FileName} by user: {UserId}", 
                    fileEvent.FileName, fileEvent.UserId);

                await Task.Delay(100);

                _logger.LogInformation("Successfully processed file upload event for file: {FileName}", 
                    fileEvent.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload event for file: {FileName}", 
                    fileEvent.FileName);
                throw;
            }
        }, $"ProcessFileUploadEvent_{fileEvent.FileName}");
    }

    public async Task ProcessFileDownloadEventAsync(FileDownloadEvent fileEvent)
    {
        await _retryService.ExecuteWithRetryAsync(async () =>
        {
            try
            {
                _logger.LogInformation("Processing file download event for file: {FileName} by user: {UserId}", 
                    fileEvent.FileName, fileEvent.UserId);

                await Task.Delay(50); 

                _logger.LogInformation("Successfully processed file download event for file: {FileName}", 
                    fileEvent.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file download event for file: {FileName}", 
                    fileEvent.FileName);
                throw;
            }
        }, $"ProcessFileDownloadEvent_{fileEvent.FileName}");
    }

    public async Task ProcessFileDeleteEventAsync(FileDeleteEvent fileEvent)
    {
        await _retryService.ExecuteWithRetryAsync(async () =>
        {
            try
            {
                _logger.LogInformation("Processing file delete event for file: {FileName} by user: {UserId}", 
                    fileEvent.FileName, fileEvent.UserId);

                await Task.Delay(75);

                _logger.LogInformation("Successfully processed file delete event for file: {FileName}", 
                    fileEvent.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file delete event for file: {FileName}", 
                    fileEvent.FileName);
                throw;
            }
        }, $"ProcessFileDeleteEvent_{fileEvent.FileName}");
    }
} 