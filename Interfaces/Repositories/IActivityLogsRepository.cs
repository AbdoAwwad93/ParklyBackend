using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IActivityLogsRepository : IGenericRepository<ActivityLog>
    {
        Task<List<ActivityLog>> GetPlatformRecentAsync(int limit);
        Task<List<ActivityLog>> GetForParkingsAsync(IEnumerable<Guid> parkingIds, int limit);
    }
}
