using System.Diagnostics;

namespace ImpalaApi.Features.Diagnostics;

/// <summary>
/// Diagnostic endpoint to simulate slow processing for graceful shutdown testing.
/// </summary>
public static class Slow
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/slow", Handler)
            .WithName("Slow")
            .WithTags("Diagnostics")
            .WithSummary("Simulate a slow request")
            .WithDescription("Delays for a few seconds to test graceful shutdown behavior")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handler(ILoggerFactory loggerFactory, HttpContext context, int delaySeconds = 8)
    {
        var logger = loggerFactory.CreateLogger("SlowEndpoint");
        var delay = Math.Clamp(delaySeconds, 1, 30);

        var sw = Stopwatch.StartNew();
        logger.LogInformation("/api/slow started with delay {DelaySeconds}s", delay);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delay), context.RequestAborted);
            sw.Stop();
            logger.LogInformation("/api/slow completed in {ElapsedMs} ms", sw.ElapsedMilliseconds);
            return Results.Ok(new
            {
                message = "Completed slow operation",
                delaySeconds = delay,
                elapsedMs = sw.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            logger.LogWarning("/api/slow cancelled after {ElapsedMs} ms", sw.ElapsedMilliseconds);
            return Results.StatusCode(499); // Client closed request
        }
    }
}
