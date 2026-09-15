using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse<NotificationPageDTO>> GetForOwnerAsync(Guid ownerId, NotificationType? type, bool? isRead, int page, int pageSize);
        Task<ApiResponse<NotificationSummaryDTO>> GetSummaryAsync(Guid ownerId);
        Task<ApiResponse> MarkReadAsync(Guid ownerId, Guid notificationId);
        Task<ApiResponse> MarkAllReadAsync(Guid ownerId);
        Task CreateAsync(Guid recipientUserId, NotificationType type, string title, string message,
            Guid? parkingId = null, Guid? reservationId = null, Guid? spaceId = null);
    }
}
