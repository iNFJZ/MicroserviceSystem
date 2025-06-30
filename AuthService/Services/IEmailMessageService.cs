using AuthService.Models;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public interface IEmailMessageService
    {
        Task PublishRegisterNotificationAsync(RegisterNotificationEmailEvent emailEvent);
    }
} 