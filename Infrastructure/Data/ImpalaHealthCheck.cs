using Microsoft.Extensions.Diagnostics.HealthChecks;
using ImpalaApi.Infrastructure.Data;
using Dapper;

namespace ImpalaApi.Infrastructure.Data;

public class ImpalaHealthCheck : IHealthCheck
{
    private readonly OdbcConnectionFactory _connectionFactory;
    private readonly ILogger<ImpalaHealthCheck> _logger;

    public ImpalaHealthCheck(OdbcConnectionFactory connectionFactory, ILogger<ImpalaHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting Impala health check");

            await using var connection = await _connectionFactory.CreateConnectionAsync();
            var result = await connection.ExecuteScalarAsync<int>("SELECT 1");

            if (result == 1)
            {
                _logger.LogDebug("Impala health check passed");
                return HealthCheckResult.Healthy("Impala database is healthy");
            }

            _logger.LogWarning("Impala health check returned unexpected value: {Result}", result);
            return HealthCheckResult.Degraded("Impala database returned unexpected result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impala health check failed: {Message}", ex.Message);
            return HealthCheckResult.Unhealthy(
                "Impala database is unavailable", 
                ex);
        }
    }
}
