using Grpc.Core;
using GrpcGreeter;
using FileService.Services;

namespace GrpcGreeter.Services
{
    public class SimpleFileGrpcService : FileService.FileServiceBase
    {
        private readonly ILogger<SimpleFileGrpcService> _logger;
        private readonly IFileService _fileService;

        public SimpleFileGrpcService(ILogger<SimpleFileGrpcService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        public override Task<UploadFileResponse> UploadFile(UploadFileRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Upload file request received: {FileName}", request.FileName);
            return Task.FromResult(new UploadFileResponse
            {
                Success = true,
                FileId = Guid.NewGuid().ToString(),
                FileName = request.FileName,
                FileUrl = $"https://storage.example.com/{request.FileName}",
                FileSize = request.FileData.Length,
                Message = "File uploaded successfully (mock)"
            });
        }

        public override Task<DownloadFileResponse> DownloadFile(DownloadFileRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Download file request received: {FileId}", request.FileId);
            
            return Task.FromResult(new DownloadFileResponse
            {
                Success = true,
                FileData = Google.Protobuf.ByteString.CopyFromUtf8("Mock file content"),
                FileName = "mock.txt",
                ContentType = "text/plain",
                Message = "File downloaded successfully (mock)"
            });
        }

        public override Task<DeleteFileResponse> DeleteFile(DeleteFileRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Delete file request received: {FileId}", request.FileId);
            
            return Task.FromResult(new DeleteFileResponse
            {
                Success = true,
                Message = "File deleted successfully (mock)"
            });
        }

        public override Task<GetFileInfoResponse> GetFileInfo(GetFileInfoRequest request, ServerCallContext context)
        {
            _logger.LogInformation("Get file info request received: {FileId}", request.FileId);
            
            return Task.FromResult(new GetFileInfoResponse
            {
                Success = true,
                FileInfo = new FileInfo
                {
                    FileId = request.FileId,
                    FileName = "mock.txt",
                    FileUrl = $"https://storage.example.com/{request.FileId}",
                    FileSize = 1024,
                    ContentType = "text/plain",
                    UploadedBy = request.UserId,
                    UploadedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastModified = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                },
                Message = "File info retrieved successfully (mock)"
            });
        }

        public override async Task<ListFilesResponse> ListFiles(ListFilesRequest request, ServerCallContext context)
        {
            _logger.LogInformation("List files request received for user: {UserId}", request.UserId);

            var files = await _fileService.ListFilesAsync();
            var response = new ListFilesResponse
            {
                Success = true,
                TotalCount = files.Count,
                CurrentPage = 1,
                TotalPages = 1,
                Message = "Files listed successfully"
            };

            foreach (var fileName in files)
            {
                response.Files.Add(new FileInfo
                {
                    FileId = fileName,
                    FileName = fileName
                });
            }

            return response;
        }
    }
} 