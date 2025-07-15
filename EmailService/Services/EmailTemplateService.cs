using System.Text;
using Newtonsoft.Json;

namespace EmailService.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly string _templatePath;
        private readonly IConfiguration _config;
        private readonly Dictionary<string, Dictionary<string, object>> _langFiles;

        public EmailTemplateService(IConfiguration config)
        {
            _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
            _config = config;
            _langFiles = LoadLangFiles();
        }

        private Dictionary<string, Dictionary<string, object>> LoadLangFiles()
        {
            var langFiles = new Dictionary<string, Dictionary<string, object>>();
            var frontendPath = _config["Frontend:Path"] ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "frontend", "assets", "lang");
            
            var languages = new[] { "en", "vi", "ja" };
            
            foreach (var lang in languages)
            {
                var langFile = Path.Combine(frontendPath, $"{lang}.json");
                if (File.Exists(langFile))
                {
                    try
                    {
                        var json = File.ReadAllText(langFile, Encoding.UTF8);
                        var langData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        langFiles[lang] = langData;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load language file {langFile}: {ex.Message}");
                    }
                }
            }
            
            return langFiles;
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

        public string GenerateVerifyEmailContent(string username, string verifyLink, string lang = null)
        {
            var template = LoadTemplate("verify-email", lang);
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username },
                { "VerifyLink", verifyLink }
            };
            return ReplacePlaceholders(template, placeholders);
        }

        public string GenerateResetPasswordContent(string username, string email, string userId, string ipAddress, string resetLink, int expiryMinutes, string lang = null)
        {
            var template = LoadTemplate("reset-password", lang);
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

        public string GenerateDeactivateAccountContent(string username, string lang = null)
        {
            var template = LoadTemplate("deactivate-account", lang);
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

        public string GenerateRestoreAccountContent(string username, DateTime restoredAt, string reason, string lang = null)
        {
            var template = LoadTemplate("restore-account", lang);
            var placeholders = new Dictionary<string, string>
            {
                { "Username", username },
                { "RestoredAt", restoredAt.ToString("dd/MM/yyyy HH:mm:ss UTC") },
                { "Reason", reason },
                { "LoginUrl", _config["Frontend:BaseUrl"] + "/auth/login.html" }
            };
            return ReplacePlaceholders(template, placeholders);
        }

        public string GetSubject(string type, string lang = null)
        {
            lang = string.IsNullOrEmpty(lang) ? "en" : lang;
            
            if (_langFiles.TryGetValue(lang, out var langData))
            {
                if (langData.TryGetValue("emailSubjects", out var emailSubjectsObj))
                {
                    if (emailSubjectsObj is Newtonsoft.Json.Linq.JObject emailSubjects)
                    {
                        if (emailSubjects.TryGetValue(type, out var subject))
                        {
                            return subject.ToString();
                        }
                    }
                }
            }
            
            if (lang != "en" && _langFiles.TryGetValue("en", out var enLangData))
            {
                if (enLangData.TryGetValue("emailSubjects", out var enEmailSubjectsObj))
                {
                    if (enEmailSubjectsObj is Newtonsoft.Json.Linq.JObject enEmailSubjects)
                    {
                        if (enEmailSubjects.TryGetValue(type, out var subject))
                        {
                            return subject.ToString();
                        }
                    }
                }
            }
            
            return type;
        }
    }
} 