namespace UserService.Services
{
    public interface IRedisKeyService
    {
        Task<IEnumerable<string>> GetKeysAsync(string pattern);
    }
} 