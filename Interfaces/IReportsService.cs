using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IReportsService
    {
        Task<ApiResponse<RevenueReportsDTO>> GetRevenueReportAsync(Guid ownerId, string? period, DateTime? from, DateTime? to);
        Task<ApiResponse<string>> ExportRevenueByLocationCsvAsync(Guid ownerId, string? period, DateTime? from, DateTime? to);
    }
}
