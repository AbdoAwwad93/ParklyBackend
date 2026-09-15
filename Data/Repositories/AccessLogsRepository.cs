using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Data.Repositories
{
    public class AccessLogsRepository : GenericRepository<AccessLog>, IAccessLogsRepository
    {
        public AccessLogsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<AccessLog>> GetRecentByParkingsAsync(IEnumerable<Guid> parkingIds, DateTime since, int take)
        {
            return await _dbSet
                .Include(l => l.Reservation)
                    .ThenInclude(r => r.ParkingSpace)
                        .ThenInclude(ps => ps.Parking)
                .Include(l => l.Reservation)
                    .ThenInclude(r => r.User)
                .Where(l =>
                    l.ScanTimestamp >= since &&
                    parkingIds.Contains(l.Reservation.ParkingSpace.ParkingId))
                .OrderByDescending(l => l.ScanTimestamp)
                .Take(take)
                .ToListAsync();
        }
    }
}
