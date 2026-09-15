using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data.Repositories
{
    public class TransactionsRepository : GenericRepository<Transaction>, ITransactionsRepository
    {
        public TransactionsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetOwnerTodayRevenueAsync(Guid ownerId, DateTime todayUtc, DateTime tomorrowUtc)
        {
            return await _dbSet
                .Where(t =>
                    t.PaymentStatus == PaymentStatus.Completed &&
                    t.Reservation.ParkingSpace.Parking.OwnerId == ownerId &&
                    t.Reservation.ArrivalTime >= todayUtc &&
                    t.Reservation.ArrivalTime < tomorrowUtc)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<List<(decimal Amount, DateTime ArrivalTime)>> GetOwnerCompletedTransactionsAsync(Guid ownerId, DateTime windowStart)
        {
            var rows = await _dbSet
                .Where(t =>
                    t.PaymentStatus == PaymentStatus.Completed &&
                    t.Reservation.ParkingSpace.Parking.OwnerId == ownerId &&
                    t.Reservation.ArrivalTime >= windowStart)
                .Select(t => new { t.Amount, t.Reservation.ArrivalTime })
                .ToListAsync();

            return rows.Select(r => (r.Amount, r.ArrivalTime)).ToList();
        }

        public async Task<int> GetOwnerCheckingInSoonCountAsync(IEnumerable<Guid> parkingIds, DateTime from, DateTime to)
        {
            return await _context.Reservations
                .Where(r =>
                    parkingIds.Contains(r.ParkingSpace.ParkingId) &&
                    r.Status == ReservationStatus.Confirmed &&
                    r.ArrivalTime >= from &&
                    r.ArrivalTime <= to)
                .CountAsync();
        }
    }
}
