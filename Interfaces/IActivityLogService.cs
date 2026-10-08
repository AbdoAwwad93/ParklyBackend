using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parkly_Backend.Models.DTOs;

namespace Parkly_Backend.Interfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(string eventType, string category, string description, Guid? actorUserId = null, string? actorName = null, Guid? targetEntityId = null, string? targetEntityType = null, Guid? parkingId = null, DateTime? createdAt = null);
        Task<List<AdminActivityFeedItemDTO>> GetAdminRecentActivityAsync(int limit);
        Task<List<ActivityFeedItemDTO>> GetOwnerRecentActivityAsync(Guid ownerId, int limit);
    }
}
