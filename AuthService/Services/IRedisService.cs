using System.Text.Json;

namespace AuthService.Services
{
    public interface IRedisService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task SetExpiryAsync(string key, TimeSpan expiry);
        Task<TimeSpan?> GetTimeToLiveAsync(string key);
        Task<IEnumerable<string>> GetKeysAsync(string pattern);
        Task<bool> SetHashAsync(string key, string field, string value);
        Task<string?> GetHashAsync(string key, string field);
        Task<Dictionary<string, string>> GetHashAllAsync(string key);
        Task<bool> DeleteHashAsync(string key, string field);
    }
} 