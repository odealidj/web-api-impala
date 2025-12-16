using ImpalaApi.Features.Tables.Models;
using ImpalaApi.Infrastructure.Repositories;

namespace ImpalaApi.Features.Tables;

public static class GetTables
{
    public record Response(IEnumerable<TableDto> Tables, int Count, DateTime Timestamp);

    /// <summary>
    /// Map the GET /api/tables endpoint
    /// </summary>
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tables", Handler)
            .WithName("GetTables")
            .WithTags("Tables")
            .WithSummary("Get all tables from Impala")
            .WithDescription("Retrieves a list of all tables in the default database")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Handler for getting all tables
    /// </summary>
    private static async Task<IResult> Handler(ITablesRepository repository, ILogger<Response> logger)
    {
        logger.LogInformation("Retrieving all tables from Impala");

        var tables = await repository.GetAllTablesAsync();
        var tablesList = tables.ToList();

        var response = new Response(tablesList, tablesList.Count, DateTime.UtcNow);

        logger.LogInformation("Successfully retrieved {Count} tables", response.Count);

        return Results.Ok(response);
    }
}
