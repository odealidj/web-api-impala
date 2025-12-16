using ImpalaApi.Features.Tables.Models;
using ImpalaApi.Infrastructure.Data;

namespace ImpalaApi.Infrastructure.Repositories;

public class TablesRepository : BaseRepository, ITablesRepository
{
    private readonly ILogger<TablesRepository> _logger;

    public TablesRepository(OdbcConnectionFactory connectionFactory, ILogger<TablesRepository> logger)
        : base(connectionFactory)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TableDto>> GetAllTablesAsync()
    {
        const string sql = "SHOW TABLES IN default";
        
        _logger.LogInformation("Executing query: {Query}", sql);
        
        try
        {
            var result = await QueryAsync<TableDto>(sql);
            _logger.LogInformation("Retrieved {Count} tables from Impala", result.Count());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve tables from Impala");
            throw;
        }
    }
}
