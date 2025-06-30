using Microsoft.AspNetCore.Http;

namespace FileService.Services
{
    public interface IFileValidationService
    {
        (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file);
        (bool IsValid, string ErrorMessage) ValidateFileSize(long fileSize, long maxSize = 10 * 1024 * 1024);
    }
} 