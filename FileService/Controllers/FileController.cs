using FileService.Services;
using FileService.DTOs;
using FileService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FileService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IMessageService _messageService;
        private readonly IFileValidationService _validationService;
        private readonly ILogger<FileController> _logger;

        public FileController(
            IFileService fileService, 
            IMessageService messageService, 
            IFileValidationService validationService,
            ILogger<FileController> logger)
        {
            _fileService = fileService;
            _messageService = messageService;
            _validationService = validationService;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            var files = request.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded");

            var results = new List<object>();
            foreach (var file in files)
            {
                var (isValid, errorMessage) = _validationService.ValidateFile(file);
                if (!isValid)
                {
                    results.Add(new { fileName = file?.FileName ?? "", message = errorMessage });
                    continue;
                }

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

                    results.Add(new { fileName = file.FileName, message = "File uploaded successfully" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
                    results.Add(new { fileName = file?.FileName, message = "Error uploading file" });
                }
            }
            return Ok(results);
        }

        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            try
            {
                var files = await _fileService.ListFilesAsync();
                if (!files.Contains(fileName))
                {
                    return NotFound(new { message = $"File '{fileName}' not found" });
                }
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
                var files = await _fileService.ListFilesAsync();
                if (!files.Contains(fileName))
                {
                    return NotFound(new { message = $"File '{fileName}' not found" });
                }
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