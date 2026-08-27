using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IParkingSpacesRepository : IGenericRepository<ParkingSpace>
    {
        Task<List<ParkingSpace>> GetAllWithParkingAsync();
        Task<List<ParkingSpace>> GetByParkingIdWithParkingAsync(Guid parkingId);
        Task<ParkingSpace?> GetByIdWithParkingAsync(Guid spaceId);
        Task<ParkingSpace?> GetOwnerSpaceAsync(Guid ownerId, Guid spaceId);
        Task<List<ParkingSpace>> GetActiveSpacesWithRulesForParkingsAsync(IEnumerable<Guid> parkingIds);
        Task<List<ParkingSpace>> GetCandidateSpacesInBoundingBoxAsync(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng, string? vehicleSize = null, decimal? maxRate = null);
        Task<bool> HasActiveReservationsAsync(Guid spaceId);
    }
}
