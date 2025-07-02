using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;
using FileService.Models;
using System;

namespace FileService.Services
{
    public class MinioFileService : IFileService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioFileService(IOptions<MinioOptions> options)
        {
            var config = options.Value;
            _minioClient = new MinioClient()
                .WithEndpoint(config.Endpoint)
                .WithCredentials(config.AccessKey, config.SecretKey)
                .WithSSL(false)
                .Build();
            _bucketName = config.BucketName!;
        }

        private async Task EnsureBucketExists()
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
            }
        }
        
        public async Task UploadFileAsync(string objectName, Stream fileStream, string contentType)
        {
            await EnsureBucketExists();
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);
            await _minioClient.PutObjectAsync(putObjectArgs);
        }

        public async Task<Stream> DownloadFileAsync(string objectName)
        {
            var ms = new MemoryStream();
            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream((stream) => stream.CopyTo(ms));
            await _minioClient.GetObjectAsync(getObjectArgs);
            ms.Position = 0;
            return ms;
        }

        public async Task DeleteFileAsync(string objectName)
        {
            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);
            await _minioClient.RemoveObjectAsync(removeObjectArgs);
        }

        public async Task<List<FileService.Models.FileInfo>> ListFilesAsync()
        {
            await EnsureBucketExists();
            var files = new List<FileService.Models.FileInfo>();
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithRecursive(true);
            
            var objects = _minioClient.ListObjectsAsync(listObjectsArgs);
            var tcs = new TaskCompletionSource<bool>();
            objects.Subscribe(
                item => files.Add(new FileService.Models.FileInfo {
                    FileName = item.Key,
                    FileUrl = $"/files/{item.Key}",
                    FileSize = (long)item.Size,
                    ContentType = string.Empty,
                    // Để lưu thông tin người upload (username/email) vào trường UploadedBy,
                    // bạn cần truyền thông tin này khi upload file (ví dụ: từ token JWT lấy username/email).
                    // Sau đó, bạn có thể lưu thông tin này vào metadata của object trên MinIO.
                    // Khi liệt kê file, bạn lấy metadata ra để gán vào UploadedBy.
                    // Ví dụ (giả sử đã lấy được metadata "uploaded-by"):
                    UploadedBy = item.UserMetadata != null && item.UserMetadata.ContainsKey("uploaded-by")
                        ? item.UserMetadata["uploaded-by"]
                        : string.Empty,
                    UploadedAt = item.LastModified.ToString()
                }),
                ex => tcs.SetException(ex),
                () => tcs.SetResult(true)
            );
            await tcs.Task;
            return files;
        }

        public async Task<FileService.Models.FileInfo> GetFileInfoAsync(string objectName)
        {
            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);
            var objectInfo = await _minioClient.StatObjectAsync(statObjectArgs);
            return new FileService.Models.FileInfo {
                FileName = objectName,
                FileUrl = $"/files/{objectName}",
                FileSize = (long)objectInfo.Size,
                ContentType = objectInfo.ContentType ?? string.Empty,
                UploadedBy = string.Empty,
                UploadedAt = objectInfo.LastModified.ToString()
            };
        }
    }
} 