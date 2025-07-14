using System.Text;

namespace EmailService.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _templatePath;
        private readonly IConfiguration _config;

        public EmailTemplateService(IConfiguration config)
        {
            _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            _config = config;
        }

        public string LoadTemplate(string templateName, string lang = null)
        {
            string templateFile = templateName + (string.IsNullOrEmpty(lang) || lang == "en" ? "" : "." + lang) + ".html";
            var templatePath = Path.Combine(_templatePath, templateFile);
            if (!File.Exists(templatePath))
            {
                templatePath = Path.Combine(_templatePath, templateName + ".html");
                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template {templateName} not found at {templatePath}");
                }
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

        public string GenerateRegisterGoogleContent(string username, string resetLink = "", string lang = null)
        {
            var template = LoadTemplate("register-google", lang);
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username },
                { "ResetLink", resetLink }
            };
            return ReplacePlaceholders(template, placeholders);
        }

        public string GenerateRestoreAccountContent(string username, DateTime restoredAt, string reason)
        {
            var template = LoadTemplate("restore-account");
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username },
                { "RestoredAt", restoredAt.ToString("dd/MM/yyyy HH:mm:ss UTC") },
                { "Reason", reason },
                { "LoginUrl", _config["Frontend:BaseUrl"] + "/auth/login.html" }
            };
            return ReplacePlaceholders(template, placeholders);
        }
    }
} 