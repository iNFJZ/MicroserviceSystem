using Grpc.Core;
using GrpcGreeter;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GrpcGreeter.Services
{
    public class SimpleEmailGrpcService : GrpcGreeter.EmailService.EmailServiceBase
    {
        private readonly ILogger<SimpleEmailGrpcService> _logger;

        public SimpleEmailGrpcService(ILogger<SimpleEmailGrpcService> logger)
        {
            _logger = logger;
        }

        public override Task<SendEmailResponse> SendEmail(SendEmailRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Mock SendEmail: {To}, Subject: {Subject}", request.To, request.Subject);
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Mock email sent to {request.To}"
            });
        }

        public override Task<SendEmailResponse> SendRegistrationNotification(RegistrationNotificationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Mock SendRegistrationNotification: {Email}", request.Email);
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Mock registration notification sent to {request.Email}"
            });
        }

        public override Task<SendEmailResponse> SendPasswordResetEmail(PasswordResetRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Mock SendPasswordResetEmail: {Email}", request.Email);
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Mock password reset email sent to {request.Email}"
            });
        }

        public override Task<SendEmailResponse> SendChangePasswordNotification(ChangePasswordNotificationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Mock SendChangePasswordNotification: {Email}", request.Email);
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Mock change password notification sent to {request.Email}"
            });
        }

        public override Task<SendEmailResponse> SendFileEventNotification(FileEventNotificationRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Mock SendFileEventNotification: {Email}, Event: {EventType}", request.Email, request.EventType);
            return Task.FromResult(new SendEmailResponse
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString(),
                Message = $"Mock file event notification sent to {request.Email} for {request.EventType} event"
            });
        }
    }
} 