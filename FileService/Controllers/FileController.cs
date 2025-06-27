using FileService.Services;
using FileService.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IMessageService _messageService;
        private readonly ILogger<FileController> _logger;

        public FileController(IFileService fileService, IMessageService messageService, ILogger<FileController> logger)
        {
            _fileService = fileService;
            _messageService = messageService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("File size exceeds the limit of 10MB");
            try
            {
                using var stream = file.OpenReadStream();
                await _fileService.UploadFileAsync(file.FileName, stream, file.ContentType);

                var uploadEvent = new FileUploadEvent
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadTime = DateTime.UtcNow,
                    UserId = "anonymous"
                };

                await _messageService.PublishFileUploadEventAsync(uploadEvent);
                _logger.LogInformation("File uploaded successfully: {FileName}", file.FileName);

                return Ok(new { file.FileName, message = "File uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
                return StatusCode(500, "Error uploading file");
            }
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                var stream = await _fileService.DownloadFileAsync(fileName);

                var downloadEvent = new FileDownloadEvent
                {
                    FileName = fileName,
                    DownloadTime = DateTime.UtcNow,
                };

                await _messageService.PublishFileDownloadEventAsync(downloadEvent);
                _logger.LogInformation("File downloaded successfully: {FileName}", fileName);

                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return StatusCode(500, "Error downloading file");
            }
        }

        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> Delete(string fileName)
        {
            try
            {
                await _fileService.DeleteFileAsync(fileName);

                var deleteEvent = new FileDeleteEvent
                {
                    FileName = fileName,
                    DeleteTime = DateTime.UtcNow,
                };

                await _messageService.PublishFileDeleteEventAsync(deleteEvent);
                _logger.LogInformation("File deleted successfully: {FileName}", fileName);

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
                return StatusCode(500, "Error deleting file");
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            try
            {
                var files = await _fileService.ListFilesAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return StatusCode(500, "Error listing files");
            }
        }
    }
} 