using System.Text;

namespace EmailService.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _templatePath;

        public EmailTemplateService()
        {
            _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        }

        public string LoadTemplate(string templateName)
        {
            var templatePath = Path.Combine(_templatePath, $"{templateName}.html");
            
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template {templateName} not found at {templatePath}");
            }

            return File.ReadAllText(templatePath, Encoding.UTF8);
        }

        public string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
        {
            var result = template;
            
            foreach (var placeholder in placeholders)
            {
                result = result.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
            }

            return result;
        }

        public string GenerateVerifyEmailContent(string username, string verifyLink)
        {
            var template = LoadTemplate("verify-email");
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username },
                { "VerifyLink", verifyLink }
            };

            return ReplacePlaceholders(template, placeholders);
        }

        public string GenerateResetPasswordContent(string username, string email, string userId, string ipAddress, string resetLink, int expiryMinutes)
        {
            var template = LoadTemplate("reset-password");
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username },
                { "Email", email },
                { "UserId", userId },
                { "IpAddress", ipAddress },
                { "ResetLink", resetLink },
                { "ExpiryMinutes", expiryMinutes.ToString() }
            };

            return ReplacePlaceholders(template, placeholders);
        }

        public string GenerateDeactivateAccountContent(string username)
        {
            var template = LoadTemplate("deactivate-account");
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username }
            };

            return ReplacePlaceholders(template, placeholders);
        }
    }
} 