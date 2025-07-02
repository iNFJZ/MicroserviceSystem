using FileService.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileService.Services
{
    public class EmailMessageService : IEmailMessageService
    {
        private readonly IConfiguration _config;

        public EmailMessageService(IConfiguration config)
        {
            _config = config;
        }

        public Task PublishFileEventNotificationAsync(FileEventEmailNotification emailEvent)
        {
            return Task.CompletedTask;
        }
    }
} 