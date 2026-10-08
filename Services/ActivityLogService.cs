using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;
using Parkly_Backend.Models;
using Parkly_Backend.Models.DTOs;

namespace Parkly_Backend.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            IUnitOfWork unitOfWork,
            ILogger<ActivityLogService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task LogAsync(
            string eventType,
            string category,
            string description,
            Guid? actorUserId = null,
            string? actorName = null,
            Guid? targetEntityId = null,
            string? targetEntityType = null,
            Guid? parkingId = null,
            DateTime? createdAt = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    EventType = eventType,
                    Category = category,
                    Description = description,
                    ActorUserId = actorUserId,
                    ActorName = actorName,
                    TargetEntityId = targetEntityId,
                    TargetEntityType = targetEntityType,
                    ParkingId = parkingId,
                    CreatedAt = createdAt ?? DateTime.UtcNow
                };

                await _unitOfWork.ActivityLogs.AddAsync(log);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write activity log: {EventType} - {Description}", eventType, description);
            }
        }

        public async Task<List<AdminActivityFeedItemDTO>> GetAdminRecentActivityAsync(int limit)
        {
            limit = Math.Clamp(limit, 1, 50);

            var logs = await _unitOfWork.ActivityLogs.GetPlatformRecentAsync(limit);
            var now = DateTime.UtcNow;
            return logs.Select(a => new AdminActivityFeedItemDTO
            {
                Type = a.EventType,
                Description = a.Description,
                Timestamp = a.CreatedAt,
                TimeAgo = FormatTimeAgo(a.CreatedAt, now)
            }).ToList();
        }

        public async Task<List<ActivityFeedItemDTO>> GetOwnerRecentActivityAsync(Guid ownerId, int limit)
        {
            limit = Math.Clamp(limit, 1, 50);

            var parkings = await _unitOfWork.Parkings.GetParkingsWithSpacesAsync();
            var parkingIds = parkings
                .Where(p => p.OwnerId == ownerId)
                .Select(p => p.ParkingId)
                .ToList();

            var logs = await _unitOfWork.ActivityLogs.GetForParkingsAsync(parkingIds, limit);
            var now = DateTime.UtcNow;
            return logs.Select(a => new ActivityFeedItemDTO
            {
                Type = a.EventType,
                Description = a.Description,
                Timestamp = a.CreatedAt,
                TimeAgo = FormatTimeAgo(a.CreatedAt, now)
            }).ToList();
        }

        private static string FormatTimeAgo(DateTime timestamp, DateTime now)
        {
            var elapsed = now - timestamp;
            if (elapsed.TotalMinutes < 1) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} min ago";
            if (elapsed.TotalHours < 24)
            {
                var h = (int)elapsed.TotalHours;
                var m = elapsed.Minutes;
                return m > 0 ? $"{h}h {m}m ago" : $"{h}h ago";
            }
            var days = (int)elapsed.TotalDays;
            if (days < 7) return $"{days}d ago";
            return timestamp.ToString("MMM dd");
        }
    }
}
