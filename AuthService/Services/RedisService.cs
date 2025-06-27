using StackExchange.Redis;
using System.Text.Json;

namespace AuthService.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = redis.GetDatabase();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            await _database.StringSetAsync(key, serializedValue, expiry);
        }

        public async Task<bool> DeleteAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }

        public async Task SetExpiryAsync(string key, TimeSpan expiry)
        {
            await _database.KeyExpireAsync(key, expiry);
        }

        public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
        {
            return await _database.KeyTimeToLiveAsync(key);
        }

        public Task<IEnumerable<string>> GetKeysAsync(string pattern)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern);
            return Task.FromResult(keys.Select(k => k.ToString()));
        }

        public async Task<bool> SetHashAsync(string key, string field, string value)
        {
            return await _database.HashSetAsync(key, field, value);
        }

        public async Task<string?> GetHashAsync(string key, string field)
        {
            var value = await _database.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<Dictionary<string, string>> GetHashAllAsync(string key)
        {
            var hashEntries = await _database.HashGetAllAsync(key);
            return hashEntries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        }

        public async Task<bool> DeleteHashAsync(string key, string field)
        {
            return await _database.HashDeleteAsync(key, field);
        }
    }
} 