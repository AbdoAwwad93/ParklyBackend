using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class ActivityLogsRepository : GenericRepository<ActivityLog>, IActivityLogsRepository
    {
        public ActivityLogsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ActivityLog>> GetPlatformRecentAsync(int limit)
        {
            return await _dbSet.AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetForParkingsAsync(IEnumerable<Guid> parkingIds, int limit)
        {
            var pIds = parkingIds.ToList();
            return await _dbSet.AsNoTracking()
                .Where(a => a.ParkingId.HasValue && pIds.Contains(a.ParkingId.Value))
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
    }
}
