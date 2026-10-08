using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly IFcmPushService _pushService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IUnitOfWork unitOfWork,
            IFcmPushService pushService,
            UserManager<AppUser> userManager,
            ILogger<NotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _pushService = pushService;
            _userManager = userManager;
            _logger = logger;
        }

        public Task<ApiResponse<NotificationPageDTO>> GetForOwnerAsync(Guid ownerId, NotificationType? type, bool? isRead, int page, int pageSize)
            => GetForUserAsync(ownerId, type, isRead, page, pageSize);

        public async Task<ApiResponse<NotificationPageDTO>> GetForUserAsync(Guid userId, NotificationType? type, bool? isRead, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user?.Role == UserRole.Admin)
            {
                var hasAny = await _unitOfWork.Notifications.AnyAsync(n => n.RecipientUserId == userId);
                if (!hasAny)
                {
                    await BackfillAdminNotificationsAsync(userId);
                }
                else
                {
                    await FixAdminNotificationTimestampsAsync(userId);
                }
            }

            var (items, totalCount) = await _unitOfWork.Notifications.GetForRecipientAsync(userId, type, isRead, page, pageSize);

            return ApiResponse<NotificationPageDTO>.Success("Notifications retrieved successfully.", new NotificationPageDTO
            {
                Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize,
                TotalCount = totalCount, TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        public async Task<ApiResponse<NotificationSummaryDTO>> GetSummaryAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user?.Role == UserRole.Admin)
            {
                var hasAny = await _unitOfWork.Notifications.AnyAsync(n => n.RecipientUserId == userId);
                if (!hasAny)
                {
                    await BackfillAdminNotificationsAsync(userId);
                }
                else
                {
                    await FixAdminNotificationTimestampsAsync(userId);
                }
            }

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

            _ = Task.Run(async () =>
            {
                try
                {
                    await _pushService.SendToUserAsync(recipientUserId, title, message,
                        new Dictionary<string, string>
                        {
                            ["notificationType"] = type.ToString(),
                            ["parkingId"] = parkingId?.ToString() ?? "",
                            ["reservationId"] = reservationId?.ToString() ?? "",
                            ["spaceId"] = spaceId?.ToString() ?? ""
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fire-and-forget FCM push failed for user {UserId}.", recipientUserId);
                }
            });
        }

        public async Task NotifyAdminsAsync(NotificationType type, string title, string message, Guid? parkingId = null, Guid? reservationId = null, Guid? spaceId = null)
        {
            var adminIds = await _userManager.Users
                .Where(u => u.Role == UserRole.Admin)
                .Select(u => u.Id)
                .ToListAsync();

            if (adminIds.Count == 0) return;

            foreach (var adminId in adminIds)
            {
                await CreateAsync(adminId, type, title, message, parkingId, reservationId, spaceId);
            }
        }

        private async Task BackfillAdminNotificationsAsync(Guid adminId)
        {
            try
            {
                var recentReservations = await _unitOfWork.Reservations.Query()
                    .Include(r => r.User)
                    .Include(r => r.ParkingSpace)
                        .ThenInclude(ps => ps.Parking)
                    .ToListAsync();

                var reservationIds = recentReservations.Select(r => r.ReservationId).ToList();

                var originalDates = await _unitOfWork.Notifications.Query()
                    .Where(n => n.ReservationId != null 
                             && reservationIds.Contains(n.ReservationId.Value) 
                             && n.RecipientUserId != adminId)
                    .GroupBy(n => n.ReservationId!.Value)
                    .Select(g => new { ReservationId = g.Key, CreatedAt = g.Min(x => x.CreatedAt) })
                    .ToDictionaryAsync(x => x.ReservationId, x => x.CreatedAt);

                var reservationsWithDates = recentReservations.Select(r =>
                {
                    var creationDate = originalDates.TryGetValue(r.ReservationId, out var dt)
                        ? dt
                        : (r.ArrivalTime <= DateTime.UtcNow ? r.ArrivalTime : DateTime.UtcNow);
                    return new { Reservation = r, CreatedAt = creationDate };
                })
                .OrderByDescending(x => x.CreatedAt)
                .Take(25)
                .ToList();

                foreach (var item in reservationsWithDates)
                {
                    var r = item.Reservation;
                    var parking = r.ParkingSpace?.Parking;
                    var bookingRef = $"PK-{r.ReservationId.ToString("N")[^4..].ToUpperInvariant()}";
                    var customer = string.IsNullOrWhiteSpace(r.User?.FullName) ? "A customer" : r.User.FullName;

                    await _unitOfWork.Notifications.AddAsync(new Notification
                    {
                        RecipientUserId = adminId,
                        Type = NotificationType.Booking,
                        Title = $"New Booking — {bookingRef}",
                        Message = $"{customer} reserved spot {r.ParkingSpace?.SpotNumber ?? "spot"} at {parking?.Name ?? "Parking"} for ${r.TotalPrice:F2}.",
                        CreatedAt = item.CreatedAt,
                        IsRead = false,
                        ParkingId = parking?.ParkingId,
                        ReservationId = r.ReservationId,
                        SpaceId = r.SpaceId
                    });
                }

                var pendingOwners = await _unitOfWork.ParkingOwners.Query()
                    .Include(po => po.User)
                    .Where(po => po.VerificationStatus == VerificationStatus.Pending)
                    .ToListAsync();

                foreach (var po in pendingOwners)
                {
                    var ownerName = po.User?.FullName ?? "New Owner";
                    await _unitOfWork.Notifications.AddAsync(new Notification
                    {
                        RecipientUserId = adminId,
                        Type = NotificationType.Alert,
                        Title = "New Owner Application",
                        Message = $"{po.CompanyName} ({ownerName}) submitted an owner application awaiting verification.",
                        CreatedAt = po.User?.CreatedAt ?? DateTime.UtcNow,
                        IsRead = false
                    });
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backfill admin notifications for user {AdminId}.", adminId);
            }
        }

        private async Task FixAdminNotificationTimestampsAsync(Guid adminId)
        {
            try
            {
                var adminNotifications = await _unitOfWork.Notifications.Query()
                    .Where(n => n.RecipientUserId == adminId && n.ReservationId != null)
                    .ToListAsync();

                if (!adminNotifications.Any()) return;

                var reservationIds = adminNotifications
                    .Select(n => n.ReservationId!.Value)
                    .Distinct()
                    .ToList();

                var originalDates = await _unitOfWork.Notifications.Query()
                    .Where(n => n.ReservationId != null 
                             && reservationIds.Contains(n.ReservationId.Value) 
                             && n.RecipientUserId != adminId)
                    .GroupBy(n => n.ReservationId!.Value)
                    .Select(g => new { ReservationId = g.Key, CreatedAt = g.Min(x => x.CreatedAt) })
                    .ToDictionaryAsync(x => x.ReservationId, x => x.CreatedAt);

                bool updated = false;
                foreach (var notif in adminNotifications)
                {
                    if (originalDates.TryGetValue(notif.ReservationId!.Value, out var trueCreatedAt))
                    {
                        if (notif.CreatedAt != trueCreatedAt)
                        {
                            notif.CreatedAt = trueCreatedAt;
                            _unitOfWork.Notifications.Update(notif);
                            updated = true;
                        }
                    }
                    else if (notif.CreatedAt > DateTime.UtcNow)
                    {
                        notif.CreatedAt = DateTime.UtcNow;
                        _unitOfWork.Notifications.Update(notif);
                        updated = true;
                    }
                }

                if (updated)
                {
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to align admin notification timestamps.");
            }
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

