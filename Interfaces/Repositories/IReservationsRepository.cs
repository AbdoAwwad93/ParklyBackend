using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models;
using Parkly_Backend.Data.Repositories;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface IReservationsRepository : IGenericRepository<Reservation>
    {
        Task<Reservation?> GetReservationWithIncludesAsync(Guid reservationId);
        Task<List<Reservation>> GetActiveReservationsByUserAsync(Guid userId);
        Task<List<Reservation>> GetAllReservationsByUserAsync(Guid userId);
        Task<List<Reservation>> GetOverlappingReservationsAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId = null);
        Task<List<Reservation>> GetOverlappingReservationsForSpacesAsync(IEnumerable<Guid> spaceIds, DateTime arrival, DateTime departure);
        Task<int> GetCheckedInCountForParkingAsync(Guid parkingId);
    }
}
