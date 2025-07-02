using Grpc.Core;
using GrpcGreeter;

namespace GrpcGreeter.Services
{
    public class SimpleEmailGrpcService : EmailService.EmailServiceBase
    {
        public override Task<SendEmailResponse> SendEmail(SendEmailRequest request, ServerCallContext context)
        {
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Email sent to {request.To} (mock)"
            });
        }

        public override Task<SendEmailResponse> SendRegistrationNotification(RegistrationNotificationRequest request, ServerCallContext context)
        {
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Registration notification sent to {request.Email} (mock)"
            });
        }

        public override Task<SendEmailResponse> SendPasswordResetEmail(PasswordResetRequest request, ServerCallContext context)
        {
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Password reset email sent to {request.Email} (mock)"
            });
        }

        public override Task<SendEmailResponse> SendChangePasswordNotification(ChangePasswordNotificationRequest request, ServerCallContext context)
        {
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Change password notification sent to {request.Email} (mock)"
            });
        }

        public override Task<SendEmailResponse> SendFileEventNotification(FileEventNotificationRequest request, ServerCallContext context)
        {
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"File event notification sent to {request.Email} (mock)"
            });
        }
    }
} 