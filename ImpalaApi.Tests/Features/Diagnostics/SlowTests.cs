using FluentAssertions;
using ImpalaApi.Features.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace ImpalaApi.Tests.Features.Diagnostics;

public class SlowTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly DefaultHttpContext _httpContext;

    public SlowTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task Handler_ShouldComplete_WhenNotCancelled()
    {
        // Arrange
        var delaySeconds = 1;

        // Act - Use reflection to invoke private Handler method
        var handlerMethod = typeof(Slow).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockLoggerFactory.Object,
            _httpContext,
            delaySeconds
        })!;

        // Assert
        result.Should().NotBeNull();

        // Extract and serialize the Value property
        var valueProperty = result.GetType().GetProperty("Value");
        var value = valueProperty!.GetValue(result)!;
        var json = JsonSerializer.Serialize(value);
        var response = JsonSerializer.Deserialize<JsonDocument>(json)!;
        
        response.RootElement.GetProperty("message").GetString().Should().Contain("Completed");
    }

    [Fact]
    public async Task Handler_ShouldClampDelaySeconds_BetweenMinAndMax()
    {
        // Arrange - Test with delay > 30 (max)
        var delaySeconds = 100;

        // Act
        var handlerMethod = typeof(Slow).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var startTime = DateTime.UtcNow;
        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockLoggerFactory.Object,
            _httpContext,
            delaySeconds
        })!;
        var endTime = DateTime.UtcNow;

        // Assert - Should complete in ~30 seconds (clamped), not 100 seconds
        var elapsed = (endTime - startTime).TotalSeconds;
        elapsed.Should().BeLessThan(35); // Allow some overhead
        elapsed.Should().BeGreaterThan(25); // Should be close to 30s
    }

    [Fact]
    public async Task Handler_ShouldReturnStatusCode499_WhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _httpContext.RequestAborted = cts.Token;
        cts.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel after 100ms

        // Act
        var handlerMethod = typeof(Slow).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockLoggerFactory.Object,
            _httpContext,
            5 // Longer delay to ensure cancellation happens
        })!;

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result as IStatusCodeHttpResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(499); // Client closed request
    }

    [Fact]
    public async Task Handler_ShouldLogStart_WhenInvoked()
    {
        // Arrange
        var delaySeconds = 1;

        // Act
        var handlerMethod = typeof(Slow).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockLoggerFactory.Object,
            _httpContext,
            delaySeconds
        })!;

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("/api/slow started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_ShouldIncludeElapsedTime_InResponse()
    {
        // Arrange
        var delaySeconds = 1;

        // Act
        var handlerMethod = typeof(Slow).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockLoggerFactory.Object,
            _httpContext,
            delaySeconds
        })!;

        // Assert
        result.Should().NotBeNull();
        
        // Extract and serialize the Value property
        var valueProperty = result.GetType().GetProperty("Value");
        var value = valueProperty!.GetValue(result)!;
        var json = JsonSerializer.Serialize(value);
        var response = JsonSerializer.Deserialize<JsonDocument>(json)!;
        
        var elapsedMs = response.RootElement.GetProperty("elapsedMs").GetInt64();
        elapsedMs.Should().BeGreaterThan(900); // Should be at least 900ms
        elapsedMs.Should().BeLessThan(1500); // But less than 1.5s
    }
}
