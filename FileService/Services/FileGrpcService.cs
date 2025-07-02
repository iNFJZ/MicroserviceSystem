using GrpcGreeter;
using Grpc.Core;
using System.Threading.Tasks;
using FileService.Services;
using System.Linq;

namespace FileService.Services
{
    public class FileGrpcService : GrpcGreeter.FileService.FileServiceBase
    {
        private readonly IFileService _fileService;
        public FileGrpcService(IFileService fileService)
        {
            _fileService = fileService;
        }

        public override async Task<UploadFileResponse> UploadFile(UploadFileRequest request, ServerCallContext context)
        {
            using var ms = new System.IO.MemoryStream(request.FileData.ToByteArray());
            await _fileService.UploadFileAsync(request.FileName, ms, request.ContentType);
            return new UploadFileResponse { Success = true, FileName = request.FileName, Message = "File uploaded successfully" };
        }

        public override async Task<DownloadFileResponse> DownloadFile(DownloadFileRequest request, ServerCallContext context)
        {
            var stream = await _fileService.DownloadFileAsync(request.FileName);
            var fileInfo = await _fileService.GetFileInfoAsync(request.FileName);
            var ms = new System.IO.MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            return new DownloadFileResponse {
                Success = true,
                FileData = Google.Protobuf.ByteString.CopyFrom(ms.ToArray()),
                FileName = fileInfo.FileName,
                ContentType = fileInfo.ContentType,
                Message = "File downloaded successfully"
            };
        }

        public override async Task<DeleteFileResponse> DeleteFile(DeleteFileRequest request, ServerCallContext context)
        {
            await _fileService.DeleteFileAsync(request.FileName);
            return new DeleteFileResponse { Success = true, Message = "File deleted successfully" };
        }

        public override async Task<GetFileInfoResponse> GetFileInfo(GetFileInfoRequest request, ServerCallContext context)
        {
            var fileInfo = await _fileService.GetFileInfoAsync(request.FileName);
            return new GetFileInfoResponse {
                Success = true,
                FileInfo = new GrpcGreeter.FileInfo {
                    FileName = fileInfo.FileName,
                    FileUrl = fileInfo.FileUrl ?? string.Empty,
                    FileSize = fileInfo.FileSize,
                    ContentType = fileInfo.ContentType ?? string.Empty,
                    UploadedBy = fileInfo.UploadedBy ?? string.Empty,
                    UploadedAt = fileInfo.UploadedAt ?? string.Empty,
                },
                Message = "File info fetched successfully"
            };
        }

        public override async Task<ListFilesResponse> ListFiles(ListFilesRequest request, ServerCallContext context)
        {
            var files = await _fileService.ListFilesAsync();
            return new ListFilesResponse {
                Success = true,
                Files = { files.Select(f => new GrpcGreeter.FileInfo {
                    FileName = f.FileName,
                    FileUrl = f.FileUrl ?? string.Empty,
                    FileSize = f.FileSize,
                    ContentType = f.ContentType ?? string.Empty,
                    UploadedBy = f.UploadedBy ?? string.Empty,
                    UploadedAt = f.UploadedAt ?? string.Empty,
                }) },
                TotalCount = files.Count,
                CurrentPage = 1,
                TotalPages = 1,
                Message = "Files listed successfully"
            };
        }
    }
} 