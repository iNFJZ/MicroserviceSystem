using Grpc.Core;
using GrpcGreeter;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GrpcGreeter.Services
{
    public class SimpleFileGrpcService : GrpcGreeter.FileService.FileServiceBase
    {
        private readonly GrpcGreeter.FileService.FileServiceClient _fileClient;
        private readonly ILogger<SimpleFileGrpcService> _logger;

        public SimpleFileGrpcService(GrpcGreeter.FileService.FileServiceClient fileClient, ILogger<SimpleFileGrpcService> logger)
        {
            _fileClient = fileClient;
            _logger = logger;
        }

        public override async Task<UploadFileResponse> UploadFile(UploadFileRequest request, ServerCallContext context)
        {
            return await _fileClient.UploadFileAsync(request);
        }

        public override async Task<DownloadFileResponse> DownloadFile(DownloadFileRequest request, ServerCallContext context)
        {
            return await _fileClient.DownloadFileAsync(request);
        }

        public override async Task<DeleteFileResponse> DeleteFile(DeleteFileRequest request, ServerCallContext context)
        {
            return await _fileClient.DeleteFileAsync(request);
        }

        public override async Task<GetFileInfoResponse> GetFileInfo(GetFileInfoRequest request, ServerCallContext context)
        {
            return await _fileClient.GetFileInfoAsync(request);
        }

        public override async Task<ListFilesResponse> ListFiles(ListFilesRequest request, ServerCallContext context)
        {
            return await _fileClient.ListFilesAsync(request);
        }
    }
} 