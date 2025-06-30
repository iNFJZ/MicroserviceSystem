using FileService.Services;
using FileService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FileService.Models;

namespace FileService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IFileValidationService _validationService;
        private readonly ILogger<FileController> _logger;

        public FileController(
            IFileService fileService, 
            IFileValidationService validationService,
            ILogger<FileController> logger)
        {
            _fileService = fileService;
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

            // Get user info from JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userEmailClaim = User.FindFirst(ClaimTypes.Email);
            var usernameClaim = User.FindFirst(ClaimTypes.Name);
            
            var userId = userIdClaim?.Value ?? "anonymous";
            var userEmail = userEmailClaim?.Value ?? "";
            var username = usernameClaim?.Value ?? "Unknown User";

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
                        UserId = userId
                    };

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