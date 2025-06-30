namespace AuthService.Services
{
    public interface IHashService
    {
        Task<bool> SetHashAsync(string key, string field, string value);
        Task<string?> GetHashAsync(string key, string field);
        Task<Dictionary<string, string>> GetHashAllAsync(string key);
        Task<bool> DeleteHashAsync(string key, string field);
    }
} 