using System.Threading.Tasks;
using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IAccessService
    {
        Task<ApiResponse> ProcessScanAsync(AccessScanDTO dto);
        Task<ApiResponse<CheckInResponseDTO>> CheckInAsync(string qrToken);
        Task<ApiResponse<CheckOutResponseDTO>> CheckOutAsync(string qrToken);
    }
}
