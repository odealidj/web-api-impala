using System.Data;
using System.Data.Odbc;

namespace ImpalaApi.Infrastructure.Data;

public class OdbcConnectionFactory : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly List<OdbcConnection> _connections = new();
    private readonly ILogger<OdbcConnectionFactory> _logger;

    public OdbcConnectionFactory(IConfiguration configuration, ILogger<OdbcConnectionFactory> logger)
    {
        _connectionString = configuration.GetConnectionString("Impala") 
            ?? throw new InvalidOperationException("Impala connection string not found in configuration");
        _logger = logger;
    }

    public async Task<OdbcConnection> CreateConnectionAsync()
    {
        try
        {
            var connection = new OdbcConnection(_connectionString);
            await connection.OpenAsync();
            
            _connections.Add(connection);
            _logger.LogDebug("ODBC connection opened. State: {State}", connection.State);
            
            return connection;
        }
        catch (OdbcException ex)
        {
            _logger.LogError(ex, "Failed to open ODBC connection to Impala. Error: {Message}", ex.Message);
            throw new InvalidOperationException("Unable to connect to Impala database. Please verify the ODBC driver is installed and connection string is correct.", ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            if (connection.State != ConnectionState.Closed)
            {
                await connection.CloseAsync();
            }
            await connection.DisposeAsync();
        }
        
        _connections.Clear();
        _logger.LogDebug("All ODBC connections disposed");
    }
}
