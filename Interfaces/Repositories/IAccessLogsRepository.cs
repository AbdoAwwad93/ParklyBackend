using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IAccessLogsRepository : IGenericRepository<AccessLog>
    {
        Task<List<AccessLog>> GetRecentByParkingsAsync(IEnumerable<Guid> parkingIds, DateTime since, int take);
    }
}
