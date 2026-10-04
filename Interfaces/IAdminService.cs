using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IAdminService
    {
        // 1. Account Management
        Task<ApiResponse> RegisterAdmin(RegisterDTO dto);

        // 2. Dashboard
        Task<ApiResponse<AdminDashboardSummaryDTO>> GetSummaryAsync();
        Task<ApiResponse<RevenueOverviewDTO>> GetRevenueOverviewAsync(string period);
        Task<ApiResponse<BookingsChartDTO>> GetBookingsChartAsync(string period);
        Task<ApiResponse<List<AdminActivityFeedItemDTO>>> GetRecentActivityAsync(int limit);
        Task<ApiResponse<List<PendingApprovalDTO>>> GetPendingApprovalsAsync(int page, int pageSize);
        Task<ApiResponse> ProcessApprovalAsync(Guid ownerId, string action);

        // 3. Users (Drivers) Management
        Task<ApiResponse<AdminDriverStatsDTO>> GetDriverStatsAsync();
        Task<ApiResponse<PagedResult<AdminDriverListItemDTO>>> GetDriversAsync(string? status, string? search, int page, int pageSize);
        Task<ApiResponse<AdminDriverDetailDTO>> GetDriverByIdAsync(Guid userId);
        Task<ApiResponse> UpdateDriverStatusAsync(Guid userId, string status);

        // 4. Parking Owners Management
        Task<ApiResponse<AdminOwnerStatsDTO>> GetOwnerStatsAsync();
        Task<ApiResponse<PagedResult<AdminOwnerListItemDTO>>> GetOwnersAsync(string? status, string? search, int page, int pageSize);
        Task<ApiResponse<AdminOwnerDetailDTO>> GetOwnerByIdAsync(Guid ownerId);
        Task<ApiResponse> UpdateOwnerStatusAsync(Guid ownerId, string status);
    }
}
