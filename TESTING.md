# Test Documentation

## Overview

This project includes comprehensive unit and integration tests to ensure all endpoints function correctly. The test suite covers:

- **Unit Tests**: Testing individual handlers and business logic
- **Integration Tests**: Testing full HTTP pipeline with WebApplicationFactory

## Test Summary

**All 16 tests passing** ✅

### Test Breakdown

#### 1. GetTables Endpoint Tests (5 tests)
- `Handler_ShouldReturnOkWithTables_WhenTablesExist` - Verifies correct response when tables are retrieved
- `Handler_ShouldReturnOkWithEmptyList_WhenNoTablesExist` - Tests empty result handling  
- `Handler_ShouldIncludeTimestamp_InResponse` - Validates timestamp is added to response
- `Handler_ShouldLogInformation_WhenRetrievingTables` - Confirms logging behavior
- `Handler_ShouldCallRepository_ExactlyOnce` - Ensures no redundant calls

**Location**: `ImpalaApi.Tests/Features/Tables/GetTablesTests.cs`

#### 2. Slow Endpoint Tests (4 tests)
- `Handler_ShouldComplete_WhenNotCancelled` - Tests normal operation
- `Handler_ShouldIncludeElapsedTime_InResponse` - Validates timing accuracy
- `Handler_ShouldReturnStatusCode499_WhenCancelled` - Tests cancellation handling
- `Handler_ShouldLogStart_WhenInvoked` - Confirms startup logging

**Location**: `ImpalaApi.Tests/Features/Diagnostics/SlowTests.cs`

#### 3. Integration Tests (7 tests)
- `GetHealth_ShouldReturnOk_WithJsonResponse` - Tests /health endpoint
- `GetTables_ShouldReturnOkOrServiceUnavailable` - Tests /api/tables with real or unavailable database
- `GetSlow_ShouldReturnOk_AfterDelay` - Tests /api/slow endpoint
- `GetSwagger_ShouldReturnOk` - Tests Swagger UI endpoint
- `GetNonExistentEndpoint_ShouldReturn404` - Tests 404 handling
- `RootPath_ShouldRedirectToSwagger` - Tests root redirect
- Additional middleware and error handling tests

**Location**: `ImpalaApi.Tests/Integration/ApiIntegrationTests.cs`

## Running Tests

### Run All Tests
```bash
cd ImpalaApi.Tests
dotnet test
```

### Run Specific Test File
```bash
dotnet test --filter "Features.Tables"
dotnet test --filter "Features.Diagnostics"
dotnet test --filter "Integration"
```

### Run with Verbose Output
```bash
dotnet test --verbosity normal
```

### Run and Measure Coverage
```bash
dotnet test /p:CollectCoverage=true
```

## Test Architecture

### Unit Tests (GetTablesTests, SlowTests)
- **Framework**: xUnit
- **Mocking**: Moq for dependency injection
- **Assertions**: FluentAssertions for readable test assertions
- **Approach**: Mock repositories and services, test handlers in isolation

Example:
```csharp
var mockRepository = new Mock<ITablesRepository>();
mockRepository.Setup(r => r.GetAllTablesAsync())
    .ReturnsAsync(new[] { new TableDto { Name = "test_table" } });

// Invoke handler with reflection and verify
```

### Integration Tests (ApiIntegrationTests)
- **Framework**: xUnit + Microsoft.AspNetCore.Mvc.Testing
- **Approach**: Use WebApplicationFactory<Program> to spin up full app
- **Benefits**: Tests real database connection, middleware pipeline, exception handling

Example:
```csharp
using var client = _factory.CreateClient();
var response = await client.GetAsync("/api/tables");
response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
```

## Test Utilities

### JSON Deserialization in Tests
Anonymous types returned by endpoints are deserialized via JsonSerializer:
```csharp
var json = JsonSerializer.Serialize(value);
var response = JsonSerializer.Deserialize<JsonDocument>(json)!;
var message = response.RootElement.GetProperty("message").GetString();
```

### Reflection for Private Methods
Endpoint handlers marked as private are invoked via reflection:
```csharp
var handlerMethod = typeof(Slow).GetMethod("Handler",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var result = await (Task<IResult>)handlerMethod!.Invoke(null, parameters)!;
```

## Known Limitations

1. **Impala Connection**: Integration tests require active Impala/Cloudera ODBC connection
   - Tests will return 503 if database unavailable
   - This is expected behavior (graceful degradation)

2. **Health Check Tests**: Complex constructor mocking avoided
   - Health checks tested via integration tests instead
   - Full pipeline tests more valuable than mocked unit tests

3. **Timing Tests**: Slow endpoint timing tests have 600ms tolerance
   - Accounts for system variance and test infrastructure overhead

## CI/CD Integration

To run tests in CI/CD pipeline:

```bash
dotnet test --verbosity minimal --logger "trx;LogFileName=test-results.trx"
```

This generates xUnit TRX format compatible with Azure DevOps, GitHub Actions, etc.

## Future Improvements

- [ ] Add performance benchmarking tests
- [ ] Add database migration tests
- [ ] Add mutation testing for code quality validation
- [ ] Add load testing for concurrent requests
- [ ] Add API contract testing (OpenAPI validation)

## Troubleshooting

### Tests Fail with "Connection refused"
**Cause**: Impala/ODBC not running
**Solution**: Integration tests gracefully handle this with 503 responses

### Tests Hang on Slow Endpoint
**Cause**: Default 5s timeout too short
**Solution**: Adjust httpClient timeout in ApiIntegrationTests if needed

### Mock Setup Errors
**Cause**: Extension methods can't be mocked in Moq
**Solution**: Mock the underlying interface properties instead of extension methods
