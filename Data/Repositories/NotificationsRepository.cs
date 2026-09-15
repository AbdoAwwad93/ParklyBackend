using Microsoft.EntityFrameworkCore;
using Parkly_Backend.Interfaces.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Data.Repositories
{
    public class NotificationsRepository : GenericRepository<Notification>, INotificationsRepository
    {
        public NotificationsRepository(AppDbContext context) : base(context) { }

        public async Task<(List<Notification> Items, int TotalCount)> GetForRecipientAsync(
            Guid recipientUserId, NotificationType? type, bool? isRead, int page, int pageSize)
        {
            var query = _dbSet.AsNoTracking().Where(n => n.RecipientUserId == recipientUserId);
            if (type.HasValue) query = query.Where(n => n.Type == type.Value);
            if (isRead.HasValue) query = query.Where(n => n.IsRead == isRead.Value);

            var totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, totalCount);
        }

        public async Task<List<(NotificationType Type, int TotalCount, int UnreadCount)>> GetSummaryAsync(Guid recipientUserId)
        {
            return await _dbSet.AsNoTracking()
                .Where(n => n.RecipientUserId == recipientUserId)
                .GroupBy(n => n.Type)
                .Select(g => new { g.Key, TotalCount = g.Count(), UnreadCount = g.Count(n => !n.IsRead) })
                .Select(x => new ValueTuple<NotificationType, int, int>(x.Key, x.TotalCount, x.UnreadCount))
                .ToListAsync();
        }

        public async Task<int> MarkAllReadAsync(Guid recipientUserId, DateTime readAt)
        {
            return await _dbSet.Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, readAt));
        }
    }
}
