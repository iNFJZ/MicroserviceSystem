using Microsoft.Extensions.Logging;
using Moq;
using WorkerService.Models;
using WorkerService.Services;
using Xunit;

namespace WorkerService.Tests;

public class FileEventProcessorTests
{
    private readonly Mock<ILogger<FileEventProcessor>> _loggerMock;
    private readonly Mock<IRetryService> _retryServiceMock;
    private readonly FileEventProcessor _processor;

    public FileEventProcessorTests()
    {
        _loggerMock = new Mock<ILogger<FileEventProcessor>>();
        _retryServiceMock = new Mock<IRetryService>();
        _processor = new FileEventProcessor(_loggerMock.Object, _retryServiceMock.Object);
    }

    [Fact]
    public async Task ProcessFileUploadEventAsync_ShouldCallRetryService()
    {
        // Arrange
        var fileEvent = new FileUploadEvent
        {
            FileName = "test.txt",
            ContentType = "text/plain",
            FileSize = 1024,
            UploadTime = DateTime.UtcNow,
            UserId = "user123"
        };

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(
            It.IsAny<Func<Task>>(), 
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.ProcessFileUploadEventAsync(fileEvent);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(
            It.IsAny<Func<Task>>(), 
            $"ProcessFileUploadEvent_{fileEvent.FileName}"), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessFileDownloadEventAsync_ShouldCallRetryService()
    {
        // Arrange
        var fileEvent = new FileDownloadEvent
        {
            FileName = "test.txt",
            DownloadTime = DateTime.UtcNow,
            UserId = "user123"
        };

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(
            It.IsAny<Func<Task>>(), 
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.ProcessFileDownloadEventAsync(fileEvent);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(
            It.IsAny<Func<Task>>(), 
            $"ProcessFileDownloadEvent_{fileEvent.FileName}"), 
            Times.Once);
    }

    [Fact]
    public async Task ProcessFileDeleteEventAsync_ShouldCallRetryService()
    {
        // Arrange
        var fileEvent = new FileDeleteEvent
        {
            FileName = "test.txt",
            DeleteTime = DateTime.UtcNow,
            UserId = "user123"
        };

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(
            It.IsAny<Func<Task>>(), 
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _processor.ProcessFileDeleteEventAsync(fileEvent);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(
            It.IsAny<Func<Task>>(), 
            $"ProcessFileDeleteEvent_{fileEvent.FileName}"), 
            Times.Once);
    }
} 