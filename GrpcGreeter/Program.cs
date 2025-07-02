using GrpcGreeter.Services;
using FileService.Services;
using FileService.Models;
using AuthService.Services;
using Grpc.AspNetCore.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.Configure<FileService.Models.MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.AddScoped<IFileService, FileService.Services.MinioFileService>();
builder.Services.AddGrpcReflection();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5219, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<SimpleFileGrpcService>();
app.MapGrpcService<SimpleEmailGrpcService>();
app.MapGrpcService<GrpcAuthService>();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "gRPC Microservice System is running. Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();



