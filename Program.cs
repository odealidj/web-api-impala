using Serilog;
using ImpalaApi.Infrastructure.Data;
using ImpalaApi.Infrastructure.Repositories;
using ImpalaApi.Features.Tables;
using ImpalaApi.Features.Diagnostics;
using ImpalaApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

// Configure Serilog (bootstrap logger)
var initialConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(initialConfig)
    .CreateLogger();

try
{
    Log.Information("Starting ImpalaApi application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog 
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    });

    // Add services to the container
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "Impala API",
            Version = "v1",
            Description = ".NET 10 Minimal API with Vertical Slice Architecture for Impala Database"
        });
    });

    // Register ODBC connection factory (scoped for per-request)
    builder.Services.AddScoped<OdbcConnectionFactory>();

    // Register repositories
    builder.Services.AddScoped<ITablesRepository, TablesRepository>();

    // Add health checks
    builder.Services.AddHealthChecks()
        .AddCheck<ImpalaHealthCheck>("impala", tags: new[] { "database", "impala" });

    var app = builder.Build();

    // Configure graceful shutdown
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("Application is shutting down gracefully...");
    });

    // Configure the HTTP request pipeline
    app.UseSerilogRequestLogging();
    app.UseExceptionHandlingMiddleware();

    // Enable Swagger in all environments for testing
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Impala API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });

    // Map endpoints using vertical slice pattern
    GetTables.MapEndpoint(app);
    Slow.MapEndpoint(app);

    // Map health check endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                timestamp = DateTime.UtcNow
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    });

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();

    Log.Information("Application configured successfully. Starting web host...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class public for integration tests
public partial class Program { }
