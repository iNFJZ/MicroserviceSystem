using System.Threading.Tasks;
using Shared.EmailModels;

namespace UserService.Services
{
    public interface IEmailMessageService
    {
        Task PublishDeactivateAccountNotificationAsync(DeactivateAccountEmailEvent emailEvent);
    }
} 