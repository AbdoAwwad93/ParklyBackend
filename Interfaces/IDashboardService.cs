using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IDashboardService
    {
 
        Task<ApiResponse<DashboardSummaryDTO>> GetSummaryAsync(Guid ownerId);
        Task<ApiResponse<RevenueOverviewDTO>> GetRevenueOverviewAsync(Guid ownerId, string period);
        Task<ApiResponse<List<LocationStatusDTO>>> GetLocationsStatusAsync(Guid ownerId);
        Task<ApiResponse<List<OwnerReservationDTO>>> GetTodaysReservationsAsync(Guid ownerId, int page, int pageSize);
        Task<ApiResponse<List<ActivityFeedItemDTO>>> GetRecentActivityAsync(Guid ownerId, int limit);
    }
}
