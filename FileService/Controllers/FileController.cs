using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using FileService.Services;
using FileService.Models;
using System.Security.Claims;
using System.Collections.Generic;
using FileService.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IEmailMessageService _emailMessageService;

        public FileController(
            IFileService fileService, 
            IFileValidationService validationService,
            ILogger<FileController> logger,
            IEmailMessageService emailMessageService)
        {
            _fileService = fileService;
            _validationService = validationService;
            _logger = logger;
            _emailMessageService = emailMessageService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            var files = request.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded");

            var userEmailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
            var userNameClaim = User.FindFirst(JwtRegisteredClaimNames.Name);
            var userEmail = userEmailClaim?.Value ?? "";
            var userName = userNameClaim?.Value ?? "";

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

                    _logger.LogInformation("File uploaded successfully: {FileName}", file.FileName);
                    results.Add(new { fileName = file.FileName, message = "File uploaded successfully" });

                    try
                    {
                        await _emailMessageService.PublishFileEventNotificationAsync(new FileEventEmailNotification
                        {
                            To = userEmail,
                            Username = userName,
                            FileName = file.FileName,
                            EventType = "Upload",
                            EventTime = DateTime.UtcNow
                        });
                        _logger.LogInformation("Email notification sent for file: {FileName}", file.FileName);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "Failed to send email notification for file: {FileName}. File was uploaded successfully.", file.FileName);
                    }
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

                try
                {
                    var userEmailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                    var userNameClaim = User.FindFirst(JwtRegisteredClaimNames.Name);
                    var userEmail = userEmailClaim?.Value ?? "";
                    var userName = userNameClaim?.Value ?? "";

                    await _emailMessageService.PublishFileEventNotificationAsync(new FileEventEmailNotification
                    {
                        To = userEmail,
                        Username = userName,
                        FileName = fileName,
                        EventType = "Download",
                        EventTime = DateTime.UtcNow
                    });
                    _logger.LogInformation("Email notification sent for download: {FileName}", fileName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send email notification for download: {FileName}. File was downloaded successfully.", fileName);
                }

                return File(stream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return StatusCode(500, "Error downloading file");
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

                try
                {
                    var userEmailClaim = User.FindFirst(ClaimTypes.Email) ?? User.FindFirst("email");
                    var userNameClaim = User.FindFirst(JwtRegisteredClaimNames.Name);
                    var userEmail = userEmailClaim?.Value ?? "";
                    var userName = userNameClaim?.Value ?? "";

                    await _emailMessageService.PublishFileEventNotificationAsync(new FileEventEmailNotification
                    {
                        To = userEmail,
                        Username = userName ?? "",
                        FileName = fileName,
                        EventType = "Delete",
                        EventTime = DateTime.UtcNow
                    });
                    _logger.LogInformation("Email notification sent for delete: {FileName}", fileName);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Failed to send email notification for delete: {FileName}. File was deleted successfully.", fileName);
                }

                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
                return StatusCode(500, "Error deleting file");
            }
        }
    }
}