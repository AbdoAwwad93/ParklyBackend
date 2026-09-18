using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        public NotificationService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public Task<ApiResponse<NotificationPageDTO>> GetForOwnerAsync(Guid ownerId, NotificationType? type, bool? isRead, int page, int pageSize)
            => GetForUserAsync(ownerId, type, isRead, page, pageSize);

        public async Task<ApiResponse<NotificationPageDTO>> GetForUserAsync(Guid userId, NotificationType? type, bool? isRead, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, totalCount) = await _unitOfWork.Notifications.GetForRecipientAsync(userId, type, isRead, page, pageSize);
            return ApiResponse<NotificationPageDTO>.Success("Notifications retrieved successfully.", new NotificationPageDTO
            {
                Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize,
                TotalCount = totalCount, TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        public async Task<ApiResponse<NotificationSummaryDTO>> GetSummaryAsync(Guid userId)
        {
            var grouped = await _unitOfWork.Notifications.GetSummaryAsync(userId);
            var byType = Enum.GetValues<NotificationType>().Select(type =>
            {
                var count = grouped.FirstOrDefault(x => x.Type == type);
                return new NotificationTypeCountDTO { Type = type, TotalCount = count.TotalCount, UnreadCount = count.UnreadCount };
            }).ToList();
            return ApiResponse<NotificationSummaryDTO>.Success("Notification summary retrieved successfully.", new NotificationSummaryDTO
            { UnreadCount = byType.Sum(x => x.UnreadCount), ByType = byType });
        }

        public async Task<ApiResponse> MarkReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _unitOfWork.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.RecipientUserId == userId);
            if (notification == null) return ApiResponse.Failure("Notification not found.");
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }
            return ApiResponse.Success("Notification marked as read.");
        }

        public async Task<ApiResponse> MarkAllReadAsync(Guid userId)
        {
            await _unitOfWork.Notifications.MarkAllReadAsync(userId, DateTime.UtcNow);
            return ApiResponse.Success("All notifications marked as read.");
        }

        public async Task CreateAsync(Guid recipientUserId, NotificationType type, string title, string message, Guid? parkingId = null, Guid? reservationId = null, Guid? spaceId = null)
        {
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                RecipientUserId = recipientUserId, Type = type, Title = title, Message = message,
                ParkingId = parkingId, ReservationId = reservationId, SpaceId = spaceId
            });
            await _unitOfWork.SaveChangesAsync();
        }

        private static NotificationDTO ToDto(Notification notification) => new()
        {
            Id = notification.NotificationId, Type = notification.Type, Title = notification.Title, Message = notification.Message,
            CreatedAt = notification.CreatedAt, IsRead = notification.IsRead, ParkingId = notification.ParkingId,
            ReservationId = notification.ReservationId, SpaceId = notification.SpaceId,
            ActionUrl = notification.ReservationId.HasValue ? $"/reservations/{notification.ReservationId}" :
                notification.SpaceId.HasValue ? $"/space-management/{notification.SpaceId}" :
                notification.ParkingId.HasValue ? $"/parking-locations/{notification.ParkingId}" : null
        };
    }
}
