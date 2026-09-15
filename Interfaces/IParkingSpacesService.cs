using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Enums;
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
        Task<ApiResponse<OwnerSpacesPageDTO>> GetOwnerSpacesAsync(Guid ownerId, Guid? parkingId, string? status, SpaceType? type, string? search, int page, int pageSize);
        Task<ApiResponse<OwnerAvailabilityDTO>> GetOwnerAvailabilityAsync(Guid ownerId, Guid? parkingId);
        Task<ApiResponse<OwnerSpaceListItemDTO>> UpdateAvailabilityAsync(Guid ownerId, Guid spaceId, UpdateSpaceAvailabilityDTO dto);
        Task RefreshSpaceStatusAsync(Guid spaceId);
    }
}
