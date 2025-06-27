using FileService.Models;

namespace FileService.Services
{
    public interface IMessageService
    {
        Task PublishFileUploadEventAsync(FileUploadEvent fileEvent);
        Task PublishFileDownloadEventAsync(FileDownloadEvent fileEvent);
        Task PublishFileDeleteEventAsync(FileDeleteEvent fileEvent);
    }
} 