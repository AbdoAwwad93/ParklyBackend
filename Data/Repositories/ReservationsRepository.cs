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
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
        }

        public async Task<List<Reservation>> GetActiveReservationsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
                .Where(r => r.UserId == userId && r.Status != ReservationStatus.Completed && r.Status != ReservationStatus.Cancelled)
                .OrderBy(r => r.ArrivalTime)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetAllReservationsByUserAsync(Guid userId)
        {
            return await _dbSet
                .Include(r => r.ParkingSpace)
                .ThenInclude(ps => ps.Parking)
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
                .CountAsync();
        }
    }
}
