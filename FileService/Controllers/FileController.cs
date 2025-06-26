using FileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly MinioFileService _minioService;

        public FileController(MinioFileService minioService)
        {
            _minioService = minioService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");
            using var stream = file.OpenReadStream();
            await _minioService.UploadFileAsync(file.FileName, stream, file.ContentType);
            return Ok(new { file.FileName });
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            var stream = await _minioService.DownloadFileAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }

        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> Delete(string fileName)
        {
            await _minioService.DeleteFileAsync(fileName);
            return Ok();
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var files = await _minioService.ListFilesAsync();
            return Ok(files);
        }
    }
} 