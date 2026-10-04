using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IParkingOwnersRepository : IGenericRepository<ParkingOwner>
    {
        Task<List<ParkingOwner>> GetPendingOwnersAsync(int page, int pageSize);
        Task<List<ParkingOwner>> GetRecentApplicationsAsync(DateTime since, int take);
        Task<ParkingOwner?> GetOwnerWithUserAsync(Guid ownerId);
    }
}
