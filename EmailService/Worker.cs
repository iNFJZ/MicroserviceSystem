using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Net.Mail;
using EmailService.Services;
using System.Threading;
using System.Globalization;
using Shared.EmailModels;
using EmailService.Models;

namespace EmailService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly IEmailTemplateService _emailTemplateService;
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
        private readonly string _deactivateAccountSubject;

        public Worker(ILogger<Worker> logger, IConfiguration config, IEmailTemplateService emailTemplateService)
        {
            _logger = logger;
            _config = config;
            _emailTemplateService = emailTemplateService;
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
            _deactivateAccountSubject = _config["EmailPolicy:DeactivateAccountSubject"] ?? "Account Deactivated - Microservice System";
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
                    else if (root.TryGetProperty("DeactivatedAt", out _))
                    {
                        var deactivateEvent = JsonSerializer.Deserialize<DeactivateAccountEmailEvent>(message);
                        if (deactivateEvent != null && !string.IsNullOrEmpty(deactivateEvent.To))
                        {
                            SendDeactivateAccountMail(deactivateEvent);
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
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            mail.IsBodyHtml = true;
            
            if (!string.IsNullOrEmpty(emailEvent.VerifyLink))
            {
                mail.Body = _emailTemplateService.GenerateVerifyEmailContent(emailEvent.Username, emailEvent.VerifyLink);
            }
            else
            {
                mail.Body = $@"<p>Dear {emailEvent.Username},</p>
<p>Welcome to <strong>Microservice System</strong>! Your account has been successfully created.</p>
<ul>
    <li>Securely upload and manage your files with our MinIO-powered storage.</li>
    <li>Register, log in, and manage your sessions using JWT authentication.</li>
    <li>Enjoy seamless and secure access to all our microservices.</li>
</ul>
<p style='color:#888;'>Registration time: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)</p>
<p>If you have any questions or need support, please contact us.</p>
<p>Best regards,<br/>Microservice System Team</p>";
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

        private void SendFileEventMail(FileEventEmailNotification emailEvent)
        {
            var eventTime = emailEvent.EventTime == DateTime.MinValue ? DateTime.UtcNow : emailEvent.EventTime;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(eventTime, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            mail.IsBodyHtml = true;
            switch (emailEvent.EventType?.ToLowerInvariant())
            {
                case "upload":
                    mail.Subject = _fileUploadSubject;
                    mail.Body = $@"<p>Dear {emailEvent.Username},</p>
<p>Your file <strong>'{emailEvent.FileName}'</strong> has been <strong>successfully uploaded</strong> to your account.</p>
<p>Upload time: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)</p>
<p>You can now manage, download, or delete your files at any time using our file management service.</p>
<p>If you did not perform this action, please review your account activity for security.</p>
<p>Thank you for using Microservice System!</p>
<p>Best regards,<br/>Microservice System Team</p>";
                    break;
                case "download":
                    mail.Subject = _fileDownloadSubject;
                    mail.Body = $@"<p>Dear {emailEvent.Username},</p>
<p>You have <strong>successfully downloaded</strong> the file <strong>'{emailEvent.FileName}'</strong>.</p>
<p>Download time: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)</p>
<p>If you did not perform this action, please review your account activity for security.</p>
<p>Thank you for using Microservice System!</p>
<p>Best regards,<br/>Microservice System Team</p>";
                    break;
                case "delete":
                    mail.Subject = _fileDeleteSubject;
                    mail.Body = $@"<p>Dear {emailEvent.Username},</p>
<p>Your file <strong>'{emailEvent.FileName}'</strong> has been <strong>deleted</strong> from your account.</p>
<p>Deletion time: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)</p>
<p>If you did not perform this action, please check your account activity or contact support.</p>
<p>Thank you for using Microservice System!</p>
<p>Best regards,<br/>Microservice System Team</p>";
                    break;
                default:
                    mail.Subject = $"File {emailEvent.EventType} Notification";
                    mail.Body = $@"<p>Dear {emailEvent.Username},</p>
<p>Your file <strong>'{emailEvent.FileName}'</strong> was <strong>{emailEvent.EventType?.ToLowerInvariant()}ed</strong> at {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time).</p>
<p>Thank you for using Microservice System!</p>
<p>Best regards,<br/>Microservice System Team</p>";
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
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            mail.IsBodyHtml = true;
            
            if (!string.IsNullOrEmpty(emailEvent.ResetLink))
            {
                mail.Body = _emailTemplateService.GenerateResetPasswordContent(
                    emailEvent.Username, 
                    emailEvent.To, 
                    emailEvent.UserId?.ToString() ?? "N/A", 
                    emailEvent.IpAddress ?? "Unknown", 
                    emailEvent.ResetLink, 
                    _resetTokenExpiryMinutes
                );
            }
            else
            {
                mail.Body = $@"<p>Dear {emailEvent.Username},</p>
<p>We received a request to <strong>reset your password</strong> for your Microservice System account.</p>
<p>Your password reset token is:</p>
<p style='font-size:18px;font-weight:bold;color:#667eea'>{emailEvent.ResetToken}</p>
<p>This token will expire in <strong>{_resetTokenExpiryMinutes} minutes</strong>.</p>
<p>If you did not request a password reset, please ignore this email or contact support immediately.</p>
<p style='color:#888;'>Request time: {vnTime:yyyy-MM-dd HH:mm:ss} (Vietnam Time)</p>
<p>To reset your password, use the following API endpoint:</p>
<pre style='background:#f8f9fa;padding:10px;border-radius:5px;'>POST /api/auth/reset-password
Body: {{ ""token"": ""your-token"", ""newPassword"": ""your-new-password"", ""confirmPassword"": ""your-new-password-confirm"" }}</pre>
<p>For security reasons, all your active sessions will be invalidated after password reset.</p>
<p>Thank you for using Microservice System!</p>
<p>Best regards,<br/>Microservice System Team</p>";
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

        private void SendChangePasswordMail(ChangePasswordEmailEvent emailEvent)
        {
            var changeAt = emailEvent.ChangeAt == DateTime.MinValue ? DateTime.UtcNow : emailEvent.ChangeAt;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(changeAt, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.Subject = _changePasswordSubject;
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            mail.IsBodyHtml = true;
            mail.Body = _emailTemplateService.ReplacePlaceholders(
                _emailTemplateService.LoadTemplate("change-password"),
                new Dictionary<string, string>
                {
                    {"Username", emailEvent.Username ?? "User"},
                    {"ChangeTime", vnTime.ToString("yyyy-MM-dd HH:mm:ss")}
                }
            );
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

        private void SendDeactivateAccountMail(DeactivateAccountEmailEvent emailEvent)
        {
            var deactivatedAt = emailEvent.DeactivatedAt == DateTime.MinValue ? DateTime.UtcNow : emailEvent.DeactivatedAt;
            var vnTime = TimeZoneInfo.ConvertTimeFromUtc(deactivatedAt, GetVietnamTimeZone());
            var mail = new MailMessage();
            mail.To.Add(emailEvent.To);
            mail.Subject = _deactivateAccountSubject;
            mail.From = new MailAddress(_smtpUser, "Microservice System");
            mail.IsBodyHtml = true;
            
            mail.Body = _emailTemplateService.GenerateDeactivateAccountContent(emailEvent.Username);
            
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
