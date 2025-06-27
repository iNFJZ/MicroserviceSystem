using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WorkerService.Configuration;
using WorkerService.Services;
using Xunit;

namespace WorkerService.Tests;

public class RetryServiceTests
{
    private readonly Mock<ILogger<RetryService>> _loggerMock;
    private readonly FileProcessingConfig _config;
    private readonly RetryService _retryService;

    public RetryServiceTests()
    {
        _loggerMock = new Mock<ILogger<RetryService>>();
        _config = new FileProcessingConfig
        {
            MaxRetryAttempts = 3,
            RetryDelaySeconds = 1
        };
        var optionsMock = new Mock<IOptions<FileProcessingConfig>>();
        optionsMock.Setup(x => x.Value).Returns(_config);
        
        _retryService = new RetryService(_loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldSucceedOnFirstTry()
    {
        // Arrange
        var operation = new Mock<Func<Task<string>>>();
        operation.Setup(x => x()).ReturnsAsync("success");

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(operation.Object, "test_operation");

        // Assert
        Assert.Equal("success", result);
        operation.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldRetryOnFailure()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new Exception("Temporary failure");
            }
            return Task.FromResult("success");
        });

        // Act
        var result = await _retryService.ExecuteWithRetryAsync(operation, "test_operation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldThrowAfterMaxRetries()
    {
        // Arrange
        var operation = new Mock<Func<Task<string>>>();
        operation.Setup(x => x()).ThrowsAsync(new Exception("Persistent failure"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _retryService.ExecuteWithRetryAsync(operation.Object, "test_operation"));

        Assert.Equal("Persistent failure", exception.Message);
        operation.Verify(x => x(), Times.Exactly(_config.MaxRetryAttempts));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_ShouldSucceedOnFirstTry()
    {
        // Arrange
        var operation = new Mock<Func<Task>>();
        operation.Setup(x => x()).Returns(Task.CompletedTask);

        // Act
        await _retryService.ExecuteWithRetryAsync(operation.Object, "test_operation");

        // Assert
        operation.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_ShouldRetryOnFailure()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task>(() =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new Exception("Temporary failure");
            }
            return Task.CompletedTask;
        });

        // Act
        await _retryService.ExecuteWithRetryAsync(operation, "test_operation");

        // Assert
        Assert.Equal(3, callCount);
    }
} 