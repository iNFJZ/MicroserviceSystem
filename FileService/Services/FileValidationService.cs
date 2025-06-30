using Microsoft.AspNetCore.Http;

namespace FileService.Services
{
    public class FileValidationService : IFileValidationService
    {
        public (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null)
                return (false, "File is null");

            if (file.Length == 0)
                return (false, "File is empty");

            var (isValid, errorMessage) = ValidateFileSize(file.Length);
            if (!isValid)
                return (false, errorMessage);

            return (true, string.Empty);
        }

        public (bool IsValid, string ErrorMessage) ValidateFileSize(long fileSize, long maxSize = 10 * 1024 * 1024)
        {
            if (fileSize > maxSize)
                return (false, $"File size exceeds the limit of {maxSize / (1024 * 1024)}MB");

            return (true, string.Empty);
        }
    }
} 