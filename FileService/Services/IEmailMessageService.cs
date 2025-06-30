using FileService.Models;
using System.Threading.Tasks;

namespace FileService.Services
{
    public interface IEmailMessageService
    {
        Task PublishFileEventNotificationAsync(FileEventEmailNotification emailEvent);
    }
} 