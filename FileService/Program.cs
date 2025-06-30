using FileService.Services;
using Minio;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using FileService.Models;
using FileService.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 80 for Docker
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "File Service API", 
        Version = "v1",
        Description = "API for file upload, download, and management"
    });
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
    c.MapType<UploadFileRequest>(() => new OpenApiSchema
    {
        Type = "object",
        Properties =
        {
            ["files"] = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                }
            }
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure MinIO
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.AddScoped<IFileService, MinioFileService>();

// Configure RabbitMQ
builder.Services.Configure<RabbitMQOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddScoped<IMessageService, RabbitMQMessageService>();
builder.Services.AddScoped<IFileEventConsumer, FileEventConsumer>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();

var app = builder.Build();

var consumer = app.Services.GetRequiredService<IFileEventConsumer>();
consumer.StartConsuming();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
