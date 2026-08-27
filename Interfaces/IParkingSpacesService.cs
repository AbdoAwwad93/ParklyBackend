using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IParkingSpacesService
    {
        Task<ApiResponse<List<ParkingSpaceResponseDTO>>> GetAllAsync();
        Task<ApiResponse<List<ParkingSpaceResponseDTO>>> GetByParkingIdAsync(Guid parkingId);
        Task<ApiResponse<ParkingSpaceResponseDTO>> GetByIdAsync(Guid spaceId);
        Task<ApiResponse<ParkingSpaceResponseDTO>> CreateAsync(Guid ownerId, CreateParkingSpaceDTO dto);
        Task<ApiResponse<ParkingSpaceResponseDTO>> UpdateAsync(Guid ownerId, Guid spaceId, UpdateParkingSpaceDTO dto);
        Task<ApiResponse> DeleteAsync(Guid ownerId, Guid spaceId);
        Task<ApiResponse<List<NearbyParkingSpaceDTO>>> GetNearbySpacesAsync(NearbyParkingQuery query);
    }
}