using Parkly_Backend.Models;

namespace Parkly_Backend.Interfaces
{
    public interface IAvailabilityService
    {
        Task<bool> IsSpaceAvailableAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId = null);
        Task<List<ParkingSpace>> GetAvailableSpacesAsync(Guid parkingId, DateTime arrival, DateTime departure);
    }
}