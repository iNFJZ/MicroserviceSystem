using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Microsoft.Extensions.Options;

namespace FileService.Services
{
    public class MinioOptions
    {
        public string? Endpoint { get; set; }
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string? BucketName { get; set; }
    }

    public class MinioFileService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioFileService(IOptions<MinioOptions> options)
        {
            var config = options.Value;
            _minioClient = new MinioClient()
                .WithEndpoint(config.Endpoint)
                .WithCredentials(config.AccessKey, config.SecretKey)
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
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
            }
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

        public async Task<List<string>> ListFilesAsync()
        {
            var files = new List<string>();
            var listObjectsArgs = new ListObjectsArgs()
                .WithBucket(_bucketName)
                .WithRecursive(true);
            
            var objects = _minioClient.ListObjectsAsync(listObjectsArgs);
            var tcs = new TaskCompletionSource<bool>();
            objects.Subscribe(
                item => files.Add(item.Key),
                ex => tcs.SetException(ex),
                () => tcs.SetResult(true)
            );
            await tcs.Task;
            return files;
        }
    }
} 