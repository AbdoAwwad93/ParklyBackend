using Parkly_Backend.Models;

namespace Parkly_Backend.Interfaces
{
    public interface IAvailabilityService
    {
        Task<bool> IsSpaceAvailableAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId = null);
        Task<List<ParkingSpace>> GetAvailableSpacesAsync(Guid parkingId, DateTime arrival, DateTime departure);
        Task<Dictionary<Guid, List<ParkingSpace>>> GetAvailableSpacesForParkingsAsync(IEnumerable<Guid> parkingIds, DateTime arrival, DateTime departure);
    }
}