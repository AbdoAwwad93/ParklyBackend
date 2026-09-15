using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data.Repositories
{
    public class SpaceTypePricingRepository : GenericRepository<SpaceTypePricing>, ISpaceTypePricingRepository
    {
        public SpaceTypePricingRepository(AppDbContext context) : base(context) { }

        public Task<List<SpaceTypePricing>> GetByParkingIdAsync(Guid parkingId) =>
            _dbSet.Where(x => x.ParkingId == parkingId).OrderBy(x => x.SpaceType).ToListAsync();

        public Task<SpaceTypePricing?> GetByParkingAndTypeAsync(Guid parkingId, SpaceType spaceType) =>
            _dbSet.FirstOrDefaultAsync(x => x.ParkingId == parkingId && x.SpaceType == spaceType);
    }
}
