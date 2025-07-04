using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Net.Mail;
using EmailService.Models;
using System.Threading;
using System.Globalization;

namespace EmailService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private IConnection _connection;
        private IModel _channel;
        private string _smtpUser;
        private string _smtpPass;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly bool _smtpEnableSsl;
        private readonly int _resetTokenExpiryMinutes;
        private readonly string _registerSubject;
        private readonly string _fileUploadSubject;
        private readonly string _fileDownloadSubject;
        private readonly string _fileDeleteSubject;
        private readonly string _resetPasswordSubject;
        private readonly string _changePasswordSubject;

        public Worker(ILogger<Worker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _smtpUser = _config["Smtp:User"];
            _smtpPass = _config["Smtp:Password"];
            _smtpHost = _config["Smtp:Host"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            _smtpEnableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");
            _resetTokenExpiryMinutes = int.Parse(_config["EmailPolicy:ResetTokenExpiryMinutes"] ?? "15");
            _registerSubject = _config["EmailPolicy:RegisterSubject"] ?? "Welcome to Microservice System!";
            _fileUploadSubject = _config["EmailPolicy:FileUploadSubject"] ?? "File Uploaded Successfully";
            _fileDownloadSubject = _config["EmailPolicy:FileDownloadSubject"] ?? "File Download Notification";
            _fileDeleteSubject = _config["EmailPolicy:FileDeleteSubject"] ?? "File Deleted Successfully";
            _resetPasswordSubject = _config["EmailPolicy:ResetPasswordSubject"] ?? "Password Reset Request - Microservice System";
            _changePasswordSubject = _config["EmailPolicy:ChangePasswordSubject"] ?? "Password Changed Successfully - Microservice System";
            Task.Run(() => InitRabbitMQ()).GetAwaiter().GetResult();
        }

        private async Task InitRabbitMQ()
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMQ:HostName"],
                Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                UserName = _config["RabbitMQ:UserName"],
                Password = _config["RabbitMQ:Password"],
                VirtualHost = _config["RabbitMQ:VirtualHost"] ?? "/"
            };
            int retry = 0;
            const int maxRetry = 10;
            const int delaySeconds = 5;
            while (true)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _channel.QueueDeclare(queue: "email.notifications", durable: true, exclusive: false, autoDelete: false);
                    break;
                }
                catch (Exception ex)
                {
                    retry++;
                    if (retry >= maxRetry)
                    {
                        throw;
                    }
                    await Task.Delay(delaySeconds * 1000);
                }
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {
                    using var doc = JsonDocument.Parse(message);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("EventType", out _))
                    {
                        var fileEvent = JsonSerializer.Deserialize<FileEventEmailNotification>(message);
                        if (fileEvent != null && !string.IsNullOrEmpty(fileEvent.To))
                        {
                            SendFileEventMail(fileEvent);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            return;
                        }
                    }
                    else if (root.TryGetProperty("ResetToken", out _))
                    {
                        var resetEvent = JsonSerializer.Deserialize<ResetPasswordEmailEvent>(message);
                        if (resetEvent != null && !string.IsNullOrEmpty(resetEvent.To))
                        {
                            SendResetPasswordMail(resetEvent);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            return;
                        }
                    }
                    else if (root.TryGetProperty("ChangeAt", out _))
                    {
                        var changeEvent = JsonSerializer.Deserialize<ChangePasswordEmailEvent>(message);
                        if (changeEvent != null && !string.IsNullOrEmpty(changeEvent.To))
                        {
                            SendChangePasswordMail(changeEvent);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            return;
                        }
                    }
                    else
                    {
                        var registerEvent = JsonSerializer.Deserialize<RegisterNotificationEmailEvent>(message);
                        if (registerEvent != null && !string.IsNullOrEmpty(registerEvent.To))
                        {
                            SendRegisterMail(registerEvent);
                            _channel.BasicAck(ea.DeliveryTag, false);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                _channel.BasicAck(ea.DeliveryTag, false);
            };
            _channel.BasicConsume(queue: "email.notifications", autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        }

        private void SendRegisterMail(RegisterNotificationEmailEvent emailEvent)
        {
            var registerAt = emailEvent.RegisterAt == DateTime.MinValue ? DateTime.UtcNow : emailEvent.RegisterAt;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(registerAt, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.Subject = _registerSubject;
            if (!string.IsNullOrEmpty(emailEvent.VerifyLink))
            {
                mail.Body = $"Hello {emailEvent.Username},\n\n" +
                           "Thank you for registering with our service. Please verify your email by clicking the link below within 1 hour:\n\n" +
                           $"{emailEvent.VerifyLink}\n\n" +
                           "If you did not register, please ignore this email.\n\n" +
                           $"Account registered at: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)\n\n" +
                           "Best regards,\nMicroservice System Team";
            }
            else
            {
                mail.Body = $"Welcome to Microservice System, {emailEvent.Username}!\n\n" +
                           "Thank you for registering with our service. Your account has been successfully created.\n\n" +
                           "You can now:\n" +
                           "- Upload and manage your files securely using our MinIO-powered storage.\n" +
                           "- Register, log in, and manage your sessions using JWT authentication.\n" +
                           "- Enjoy seamless and secure access to all our microservices.\n\n" +
                           $"Account registered at: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)\n\n" +
                           "If you have any questions or need support, feel free to contact us.\n\n" +
                           "Best regards,\nMicroservice System Team";
            }
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            try
            {
                using var smtp = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = _smtpEnableSsl
                };
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void SendFileEventMail(FileEventEmailNotification emailEvent)
        {
            var eventTime = emailEvent.EventTime == DateTime.MinValue ? DateTime.UtcNow : emailEvent.EventTime;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(eventTime, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            switch (emailEvent.EventType?.ToLowerInvariant())
            {
                case "upload":
                    mail.Subject = _fileUploadSubject;
                    mail.Body = $"Hello {emailEvent.Username},\n\n" +
                                $"Your file '{emailEvent.FileName}' has been uploaded successfully at {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time).\n\n" +
                                "You can now manage your files, download, or delete them anytime using our file management service.\n\n" +
                                "Thank you for using Microservice System!\n\n" +
                                "Best regards,\nMicroservice System Team";
                    break;
                case "download":
                    mail.Subject = _fileDownloadSubject;
                    mail.Body = $"Hello {emailEvent.Username},\n\n" +
                                $"You have successfully downloaded the file '{emailEvent.FileName}' at {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time).\n\n" +
                                "If you did not perform this action, please review your account activity for security.\n\n" +
                                "Thank you for using Microservice System!\n\n" +
                                "Best regards,\nMicroservice System Team";
                    break;
                case "delete":
                    mail.Subject = _fileDeleteSubject;
                    mail.Body = $"Hello {emailEvent.Username},\n\n" +
                                $"Your file '{emailEvent.FileName}' has been deleted successfully at {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time).\n\n" +
                                "If you did not perform this action, please check your account activity or contact support.\n\n" +
                                "Thank you for using Microservice System!\n\n" +
                                "Best regards,\nMicroservice System Team";
                    break;
                default:
                    mail.Subject = $"File {emailEvent.EventType} Notification";
                    mail.Body = $"Hello {emailEvent.Username},\n\n" +
                                $"Your file '{emailEvent.FileName}' was {emailEvent.EventType?.ToLowerInvariant()}ed successfully at {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time).\n\n" +
                                "Thank you for using Microservice System!\n\n" +
                                "Best regards,\nMicroservice System Team";
                    break;
            }
            try
            {
                using var smtp = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = _smtpEnableSsl
                };
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void SendResetPasswordMail(ResetPasswordEmailEvent emailEvent)
        {
            var requestedAt = emailEvent.RequestedAt == DateTime.MinValue ? DateTime.UtcNow : emailEvent.RequestedAt;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(requestedAt, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.Subject = _resetPasswordSubject;
            mail.Body = $"Hello {emailEvent.Username},\n\n" +
                        "We received a request to reset your password for your Microservice System account.\n\n" +
                        "Your password reset token is:\n" +
                        $"{emailEvent.ResetToken}\n\n" +
                        "Please use this token to reset your password. This token will expire in {_resetTokenExpiryMinutes} minutes.\n\n" +
                        "If you did not request this password reset, please ignore this email or contact support immediately.\n\n" +
                        $"Request made at: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)\n\n" +
                        "To reset your password, use the following API endpoint:\n" +
                        "POST /api/auth/reset-password\n" +
                        "Body: {\"token\": \"your-token\", \"newPassword\": \"your-new-password\"}\n\n" +
                        "For security reasons, all your active sessions will be invalidated after password reset.\n\n" +
                        "Thank you for using Microservice System!\n\n" +
                        "Best regards,\nMicroservice System Team";
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            try
            {
                using var smtp = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = _smtpEnableSsl
                };
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void SendChangePasswordMail(ChangePasswordEmailEvent emailEvent)
        {
            var changeAt = emailEvent.ChangeAt == DateTime.MinValue ? DateTime.UtcNow : emailEvent.ChangeAt;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(changeAt, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.Subject = _changePasswordSubject;
            mail.Body = $"Hello {emailEvent.Username},\n\n" +
                        "Your password has been successfully changed for your Microservice System account.\n\n" +
                        "If you did not request this password change, please contact support immediately.\n\n" +
                        $"Request made at: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)\n\n" +
                        "For security reasons, all your active sessions will be invalidated after password change.\n\n" +
                        "Thank you for using Microservice System!\n\n" +
                        "Best regards,\nMicroservice System Team";
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            try
            {
                using var smtp = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = _smtpEnableSsl
                };
                smtp.Send(mail); 
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
