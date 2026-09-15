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
        Task<Reservation?> GetByQrCodeWithIncludesAsync(string qrCode);
        Task<bool> IsQrCodeInUseAsync(string qrCode);
        Task<List<Reservation>> GetOwnerTodaysReservationsAsync(Guid ownerId, int page, int pageSize);
        Task<int> GetOwnerActiveReservationsCountAsync(Guid ownerId);
        Task<int> GetOwnerTodayBookingsCountAsync(Guid ownerId);
        Task<List<Reservation>> GetRecentByParkingsAsync(IEnumerable<Guid> spaceIds, DateTime since, int take);
        Task<decimal> GetOwnerTodayRevenueAsync(Guid ownerId, DateTime todayUtc, DateTime tomorrowUtc);
        Task<List<(decimal TotalPrice, DateTime ArrivalTime)>> GetOwnerRevenueInWindowAsync(Guid ownerId, DateTime windowStart);
        Task<int> GetOwnerCheckingInSoonCountAsync(IEnumerable<Guid> parkingIds, DateTime from, DateTime to);
    }
}
