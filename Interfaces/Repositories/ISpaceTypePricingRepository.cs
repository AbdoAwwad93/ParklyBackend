using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface ISpaceTypePricingRepository : IGenericRepository<SpaceTypePricing>
    {
        Task<List<SpaceTypePricing>> GetByParkingIdAsync(Guid parkingId);
        Task<SpaceTypePricing?> GetByParkingAndTypeAsync(Guid parkingId, SpaceType spaceType);
    }
}
