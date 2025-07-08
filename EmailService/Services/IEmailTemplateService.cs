namespace EmailService.Services
{
    public interface IEmailTemplateService
    {
        string LoadTemplate(string templateName);
        string ReplacePlaceholders(string template, Dictionary<string, string> placeholders);
        string GenerateVerifyEmailContent(string username, string verifyLink);
        string GenerateResetPasswordContent(string username, string email, string userId, string ipAddress, string resetLink, int expiryMinutes);
        string GenerateDeactivateAccountContent(string username);
        string GenerateRegisterGoogleContent(string username, string resetLink = "");
    }
} 