using System.Data.Odbc;
using Dapper;

namespace ImpalaApi.Infrastructure.Data;

public abstract class BaseRepository : IRepository
{
    private readonly OdbcConnectionFactory _connectionFactory;

    protected BaseRepository(OdbcConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.QueryAsync<T>(sql, param);
        }
        catch (OdbcException ex)
        {
            throw new InvalidOperationException($"Query execution failed: {ex.Message}", ex);
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }
        catch (OdbcException ex)
        {
            throw new InvalidOperationException($"Query execution failed: {ex.Message}", ex);
        }
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.ExecuteAsync(sql, param);
        }
        catch (OdbcException ex)
        {
            throw new InvalidOperationException($"Command execution failed: {ex.Message}", ex);
        }
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync();
            return await connection.ExecuteScalarAsync<T>(sql, param);
        }
        catch (OdbcException ex)
        {
            throw new InvalidOperationException($"Scalar query execution failed: {ex.Message}", ex);
        }
    }
}
