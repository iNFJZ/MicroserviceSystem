using EmailService.Models;

namespace EmailService.Services;

public interface IEmailTemplateService
{
    string LoadTemplate(string templateName, string lang = null);
    string ReplacePlaceholders(string template, Dictionary<string, string> placeholders);
    string GenerateVerifyEmailContent(string username, string verifyLink, string lang = null);
    string GenerateResetPasswordContent(string username, string email, string userId, string ipAddress, string resetLink, int expiryMinutes, string lang = null);
    string GenerateDeactivateAccountContent(string username, string lang = null);
    string GenerateRegisterGoogleContent(string username, string resetLink = "", string lang = null);
    string GenerateRestoreAccountContent(string username, DateTime restoredAt, string reason, string lang = null);
    string GetSubject(string type, string lang = null);
} 