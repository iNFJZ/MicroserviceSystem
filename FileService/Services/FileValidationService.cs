using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace FileService.Services
{
    public class FileValidationService : IFileValidationService
    {
        private readonly IStringLocalizer<FileValidationService> _localizer;
        public FileValidationService(IStringLocalizer<FileValidationService> localizer)
        {
            _localizer = localizer;
        }

        public (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null)
                return (false, _localizer["FileIsNull"]);

            if (file.Length == 0)
                return (false, _localizer["FileIsEmpty"]);

            var (isValid, errorMessage) = ValidateFileSize(file.Length);
            if (!isValid)
                return (false, errorMessage);

            return (true, string.Empty);
        }

        public (bool IsValid, string ErrorMessage) ValidateFileSize(long fileSize, long maxSize = 10 * 1024 * 1024)
        {
            if (fileSize > maxSize)
                return (false, string.Format(_localizer["FileSizeExceedsLimit"], maxSize / (1024 * 1024)));

            return (true, string.Empty);
        }
    }
} 