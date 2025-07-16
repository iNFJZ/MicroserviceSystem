namespace AuthService.DTOs
{
    public class RegisterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
    }
} 