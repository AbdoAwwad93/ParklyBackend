using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface ITransactionsRepository : IGenericRepository<Transaction>
    {
        Task<decimal> GetOwnerTodayRevenueAsync(Guid ownerId, DateTime todayUtc, DateTime tomorrowUtc);
        Task<List<(decimal Amount, DateTime ArrivalTime)>> GetOwnerCompletedTransactionsAsync(Guid ownerId, DateTime windowStart);
        Task<int> GetOwnerCheckingInSoonCountAsync(IEnumerable<Guid> parkingIds, DateTime from, DateTime to);
    }
}
