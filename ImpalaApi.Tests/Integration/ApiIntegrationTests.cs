using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

namespace ImpalaApi.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk_WithJsonResponse()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetTables_ShouldReturnOkOrServiceUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/api/tables");

        // Assert - Either OK (if Impala connected) or 503 (if not connected)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetSlow_ShouldReturnOk_AfterDelay()
    {
        // Arrange
        var delaySeconds = 2;
        var startTime = DateTime.UtcNow;

        // Act
        var response = await _client.GetAsync($"/api/slow?delaySeconds={delaySeconds}");
        var endTime = DateTime.UtcNow;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var elapsed = (endTime - startTime).TotalSeconds;
        elapsed.Should().BeGreaterThan(delaySeconds - 0.5);
        elapsed.Should().BeLessThan(delaySeconds + 1);

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Contain("Completed");
    }

    [Fact]
    public async Task RootPath_ShouldRedirectToSwagger()
    {
        // Act
        var response = await _client.GetAsync("/", HttpCompletionOption.ResponseHeadersRead);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.MovedPermanently, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task GetSwagger_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Impala API");
    }

    [Fact]
    public async Task GetNonExistentEndpoint_ShouldReturn404()
    {
        // Act
        var response = await _client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
