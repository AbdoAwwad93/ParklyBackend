using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<ApiResponse<AdminDashboardSummaryDTO>> GetSummaryAsync();
        Task<ApiResponse<RevenueOverviewDTO>> GetRevenueOverviewAsync(string period);
        Task<ApiResponse<BookingsChartDTO>> GetBookingsChartAsync(string period);
        Task<ApiResponse<List<AdminActivityFeedItemDTO>>> GetRecentActivityAsync(int limit);
        Task<ApiResponse<List<PendingApprovalDTO>>> GetPendingApprovalsAsync(int page, int pageSize);
        Task<ApiResponse> ProcessApprovalAsync(Guid ownerId, string action);
    }
}
