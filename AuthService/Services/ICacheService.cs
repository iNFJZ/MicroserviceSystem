namespace AuthService.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<bool> DeleteAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task SetExpiryAsync(string key, TimeSpan expiry);
        Task<TimeSpan?> GetTimeToLiveAsync(string key);
    }
} 