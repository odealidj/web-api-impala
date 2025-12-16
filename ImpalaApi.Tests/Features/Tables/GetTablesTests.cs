using FluentAssertions;
using ImpalaApi.Features.Tables;
using ImpalaApi.Features.Tables.Models;
using ImpalaApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImpalaApi.Tests.Features.Tables;

public class GetTablesTests
{
    private readonly Mock<ITablesRepository> _mockRepository;
    private readonly Mock<ILogger<GetTables.Response>> _mockLogger;

    public GetTablesTests()
    {
        _mockRepository = new Mock<ITablesRepository>();
        _mockLogger = new Mock<ILogger<GetTables.Response>>();
    }

    [Fact]
    public async Task Handler_ShouldReturnOkWithTables_WhenTablesExist()
    {
        // Arrange
        var expectedTables = new List<TableDto>
        {
            new() { Name = "table1", Type = "TABLE", Comment = "Test table 1" },
            new() { Name = "table2", Type = "TABLE", Comment = "Test table 2" },
            new() { Name = "table3", Type = "VIEW", Comment = null }
        };

        _mockRepository
            .Setup(r => r.GetAllTablesAsync())
            .ReturnsAsync(expectedTables);

        // Act - Use reflection to invoke private Handler method
        var handlerMethod = typeof(GetTables).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockRepository.Object,
            _mockLogger.Object
        })!;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Ok<GetTables.Response>>();

        var okResult = result as Ok<GetTables.Response>;
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.Tables.Should().HaveCount(3);
        okResult.Value.Count.Should().Be(3);
        okResult.Value.Tables.Should().BeEquivalentTo(expectedTables);
    }

    [Fact]
    public async Task Handler_ShouldReturnOkWithEmptyList_WhenNoTablesExist()
    {
        // Arrange
        var emptyTables = new List<TableDto>();

        _mockRepository
            .Setup(r => r.GetAllTablesAsync())
            .ReturnsAsync(emptyTables);

        // Act
        var handlerMethod = typeof(GetTables).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockRepository.Object,
            _mockLogger.Object
        })!;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Ok<GetTables.Response>>();

        var okResult = result as Ok<GetTables.Response>;
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.Tables.Should().BeEmpty();
        okResult.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handler_ShouldIncludeTimestamp_InResponse()
    {
        // Arrange
        var tables = new List<TableDto>
        {
            new() { Name = "test_table", Type = "TABLE", Comment = null }
        };

        _mockRepository
            .Setup(r => r.GetAllTablesAsync())
            .ReturnsAsync(tables);

        var beforeCall = DateTime.UtcNow;

        // Act
        var handlerMethod = typeof(GetTables).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockRepository.Object,
            _mockLogger.Object
        })!;

        var afterCall = DateTime.UtcNow;

        // Assert
        var okResult = result as Ok<GetTables.Response>;
        okResult!.Value!.Timestamp.Should().BeOnOrAfter(beforeCall);
        okResult.Value.Timestamp.Should().BeOnOrBefore(afterCall);
    }

    [Fact]
    public async Task Handler_ShouldLogInformation_WhenRetrievingTables()
    {
        // Arrange
        var tables = new List<TableDto>
        {
            new() { Name = "table1", Type = "TABLE", Comment = null }
        };

        _mockRepository
            .Setup(r => r.GetAllTablesAsync())
            .ReturnsAsync(tables);

        // Act
        var handlerMethod = typeof(GetTables).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockRepository.Object,
            _mockLogger.Object
        })!;

        // Assert - Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieving all tables")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handler_ShouldCallRepository_ExactlyOnce()
    {
        // Arrange
        var tables = new List<TableDto>();
        _mockRepository
            .Setup(r => r.GetAllTablesAsync())
            .ReturnsAsync(tables);

        // Act
        var handlerMethod = typeof(GetTables).GetMethod("Handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        await (Task<IResult>)handlerMethod!.Invoke(null, new object[]
        {
            _mockRepository.Object,
            _mockLogger.Object
        })!;

        // Assert
        _mockRepository.Verify(r => r.GetAllTablesAsync(), Times.Once);
    }
}
