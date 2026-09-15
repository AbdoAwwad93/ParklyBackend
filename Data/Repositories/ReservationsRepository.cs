using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data.Repositories
{
    public class ReservationsRepository : GenericRepository<Reservation>, IReservationsRepository
    {
        public ReservationsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Reservation?> GetReservationWithIncludesAsync(Guid reservationId)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
                .Include(r => r.User)
                .Include(r => r.Review)
                .Include(r => r.AccessLogs)
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
        }

        public async Task<List<Reservation>> GetActiveReservationsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
                .Include(r => r.AccessLogs)
                .Where(r => r.UserId == userId && r.Status != ReservationStatus.Completed && r.Status != ReservationStatus.Cancelled)
                .OrderBy(r => r.ArrivalTime)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetAllReservationsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
                .Include(r => r.AccessLogs)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ArrivalTime)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetOverlappingReservationsAsync(Guid spaceId, DateTime arrival, DateTime departure, Guid? excludeReservationId = null)
        {
            var query = _dbSet.Where(r =>
                r.SpaceId == spaceId &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.Completed &&
                r.ArrivalTime < departure &&
                r.DepartureTime > arrival);

            if (excludeReservationId.HasValue)
            {
                query = query.Where(r => r.ReservationId != excludeReservationId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<List<Reservation>> GetOverlappingReservationsForSpacesAsync(IEnumerable<Guid> spaceIds, DateTime arrival, DateTime departure)
        {
            return await _dbSet.Where(r =>
                spaceIds.Contains(r.SpaceId) &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.Completed &&
                r.ArrivalTime < departure &&
                r.DepartureTime > arrival)
                .ToListAsync();
        }

        public async Task<int> GetCheckedInCountForParkingAsync(Guid parkingId)
        {
            return await _dbSet
                .Where(r => r.ParkingSpace.ParkingId == parkingId && r.Status == ReservationStatus.CheckedIn)
                .Select(r => r.SpaceId)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> GetReservedCountForParkingAsync(Guid parkingId, DateTime now)
        {
            return await _dbSet
                .Where(r => r.ParkingSpace.ParkingId == parkingId
                    && r.Status == ReservationStatus.Confirmed
                    && r.ArrivalTime <= now
                    && r.DepartureTime > now)
                .Select(r => r.SpaceId)
                .Distinct()
                .CountAsync();
        }

        public async Task<decimal> GetParkingMonthRevenueAsync(Guid parkingId, DateTime monthStart, DateTime monthEnd)
        {
            return await _dbSet
                .Where(r => r.ParkingSpace.ParkingId == parkingId
                    && r.Status == ReservationStatus.Completed
                    && r.DepartureTime >= monthStart
                    && r.DepartureTime < monthEnd)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;
        }

        public async Task<Reservation?> GetByQrCodeWithIncludesAsync(string qrCode)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
                .Include(r => r.User)
                .Include(r => r.Review)
                .Include(r => r.AccessLogs)
                .Where(r => r.QrCode == qrCode)
                .OrderByDescending(r => r.ArrivalTime)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsQrCodeInUseAsync(string qrCode)
        {
            return await _dbSet
                .AnyAsync(r => r.QrCode == qrCode &&
                    (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn));
        }

        public async Task<List<Reservation>> GetOwnerTodaysReservationsAsync(Guid ownerId, int page, int pageSize)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);

            return await _dbSet
                .Include(r => r.ParkingSpace)
                    .ThenInclude(ps => ps.Parking)
                .Include(r => r.User)
                .Where(r =>
                    r.ParkingSpace.Parking.OwnerId == ownerId &&
                    r.ArrivalTime < tomorrowUtc &&
                    r.DepartureTime > todayUtc)
                .OrderBy(r => r.ArrivalTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetOwnerActiveReservationsCountAsync(Guid ownerId)
        {
            return await _dbSet
                .Where(r =>
                    r.ParkingSpace.Parking.OwnerId == ownerId &&
                    (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn))
                .CountAsync();
        }

        public async Task<int> GetOwnerTodayBookingsCountAsync(Guid ownerId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var tomorrowUtc = todayUtc.AddDays(1);
            return await _dbSet
                .Where(r =>
                    r.ParkingSpace.Parking.OwnerId == ownerId &&
                    r.ArrivalTime >= todayUtc &&
                    r.ArrivalTime < tomorrowUtc)
                .CountAsync();
        }

        public async Task<List<Reservation>> GetRecentByParkingsAsync(IEnumerable<Guid> parkingIds, DateTime since, int take)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                    .ThenInclude(ps => ps.Parking)
                .Include(r => r.User)
                .Where(r =>
                    r.ArrivalTime >= since &&
                    parkingIds.Contains(r.ParkingSpace.ParkingId))
                .OrderByDescending(r => r.ArrivalTime)
                .Take(take)
                .ToListAsync();
        }

        public async Task<decimal> GetOwnerTodayRevenueAsync(Guid ownerId, DateTime todayUtc, DateTime tomorrowUtc)
        {
            return await _dbSet
                .Where(r =>
                    r.ParkingSpace.Parking.OwnerId == ownerId &&
                    r.Status == ReservationStatus.Completed &&
                    r.DepartureTime >= todayUtc &&
                    r.DepartureTime < tomorrowUtc)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0m;
        }

        public async Task<List<(decimal TotalPrice, DateTime ArrivalTime)>> GetOwnerRevenueInWindowAsync(Guid ownerId, DateTime windowStart)
        {
            var rows = await _dbSet
                .Where(r =>
                    r.ParkingSpace.Parking.OwnerId == ownerId &&
                    r.Status == ReservationStatus.Completed &&
                    r.ArrivalTime >= windowStart)
                .Select(r => new { r.TotalPrice, r.ArrivalTime })
                .ToListAsync();

            return rows.Select(r => (r.TotalPrice, r.ArrivalTime)).ToList();
        }

        public async Task<int> GetOwnerCheckingInSoonCountAsync(IEnumerable<Guid> parkingIds, DateTime from, DateTime to)
        {
            return await _dbSet
                .Where(r =>
                    parkingIds.Contains(r.ParkingSpace.ParkingId) &&
                    r.Status == ReservationStatus.Confirmed &&
                    r.ArrivalTime >= from &&
                    r.ArrivalTime <= to)
                .CountAsync();
        }
    }
}
