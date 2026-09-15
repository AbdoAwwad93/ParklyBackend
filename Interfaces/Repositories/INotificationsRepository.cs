using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Models;
using Parkly_Backend.Models.Enums;

namespace Parkly_Backend.Interfaces.Repositories
{
    public interface INotificationsRepository : IGenericRepository<Notification>
    {
        Task<(List<Notification> Items, int TotalCount)> GetForRecipientAsync(
            Guid recipientUserId, NotificationType? type, bool? isRead, int page, int pageSize);
        Task<List<(NotificationType Type, int TotalCount, int UnreadCount)>> GetSummaryAsync(Guid recipientUserId);
        Task<int> MarkAllReadAsync(Guid recipientUserId, DateTime readAt);
    }
}
