using AuthService.Models;
using System.Threading.Tasks;
using Shared.EmailModels;

namespace AuthService.Services
{
    public interface IEmailMessageService
    {
        Task PublishRegisterNotificationAsync(RegisterNotificationEmailEvent emailEvent);
        Task PublishResetPasswordNotificationAsync(ResetPasswordEmailEvent emailEvent);
        Task PublishChangePasswordNotificationAsync(ChangePasswordEmailEvent emailEvent);
        Task PublishDeactivateAccountNotificationAsync(DeactivateAccountEmailEvent emailEvent);
        Task PublishRegisterGoogleNotificationAsync(RegisterGoogleNotificationEmailEvent emailEvent);
    }
} 