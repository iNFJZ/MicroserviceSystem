namespace AuthService.Services
{
    public interface IHunterEmailVerifierService
    {
        Task<bool> VerifyEmailAsync(string email);
    }
} 