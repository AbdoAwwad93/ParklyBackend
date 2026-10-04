using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data.Repositories
{
    public class ParkingOwnersRepository : GenericRepository<ParkingOwner>, IParkingOwnersRepository
    {
        public ParkingOwnersRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ParkingOwner>> GetPendingOwnersAsync(int page, int pageSize)
        {
            return await _dbSet
                .Include(o => o.User)
                .Where(o => o.VerificationStatus == VerificationStatus.Pending)
                .OrderBy(o => o.User.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ParkingOwner>> GetRecentApplicationsAsync(DateTime since, int take)
        {
            return await _dbSet
                .Include(o => o.User)
                .Where(o => o.VerificationStatus == VerificationStatus.Pending && o.User.CreatedAt >= since)
                .OrderByDescending(o => o.User.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<ParkingOwner?> GetOwnerWithUserAsync(Guid ownerId)
        {
            return await _dbSet
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OwnerId == ownerId);
        }
    }
}
