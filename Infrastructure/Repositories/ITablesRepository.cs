using ImpalaApi.Features.Tables.Models;

namespace ImpalaApi.Infrastructure.Repositories;


public interface ITablesRepository
{

    Task<IEnumerable<TableDto>> GetAllTablesAsync();
}
