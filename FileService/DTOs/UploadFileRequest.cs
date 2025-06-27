using Microsoft.AspNetCore.Http;

namespace FileService.DTOs
{
    public class UploadFileRequest
    {
        public required List<IFormFile> Files { get; set; }
    }
} 