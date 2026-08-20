using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IParkingsService
    {
        Task<ApiResponse<List<ParkingResponseDTO>>> GetAllAsync();
        Task<ApiResponse<ParkingResponseDTO>> GetByIdAsync(Guid id);
        Task<ApiResponse<ParkingResponseDTO>> CreateAsync(Guid ownerId, CreateParkingDTO dto);
        Task<ApiResponse<ParkingResponseDTO>> UpdateAsync(Guid ownerId, Guid id, UpdateParkingDTO dto);
        Task<ApiResponse> DeleteAsync(Guid ownerId, Guid id);
        Task<ApiResponse<List<SearchParkingDTO>>> SearchAsync(SearchParkingQuery query);
    }
}