using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthService.Services
{
    public class HunterEmailVerifierService : IHunterEmailVerifierService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public HunterEmailVerifierService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["HunterApiKey"] ?? throw new ArgumentNullException(nameof(config), "HunterApiKey configuration is required");
        }

        public async Task<bool> VerifyEmailAsync(string email)
        {
            var url = $"https://api.hunter.io/v2/email-verifier?email={email}&api_key={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return false;
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("result", out var result))
                {
                    var resultStr = result.GetString();
                    return resultStr == "deliverable";
                }
            }
            return false;
        }
    }
} 