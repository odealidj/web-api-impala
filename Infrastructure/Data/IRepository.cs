namespace ImpalaApi.Infrastructure.Data;


public interface IRepository
{
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);

    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);

    Task<int> ExecuteAsync(string sql, object? param = null);

    Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null);
}
