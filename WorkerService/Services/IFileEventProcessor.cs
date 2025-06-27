using WorkerService.Models;

namespace WorkerService.Services;

public interface IFileEventProcessor
{
    Task ProcessFileUploadEventAsync(FileUploadEvent fileEvent);
    Task ProcessFileDownloadEventAsync(FileDownloadEvent fileEvent);
    Task ProcessFileDeleteEventAsync(FileDeleteEvent fileEvent);
} 