using Parkly_Backend.Models.DTOs;
using Parkly_Backend.Models.Response;

namespace Parkly_Backend.Interfaces
{
    public interface IParkingsService
    {
        Task<ApiResponse<List<ParkingResponseDTO>>> GetAllAsync();
        Task<ApiResponse<List<ParkingResponseDTO>>> GetOwnedAsync(Guid ownerId);
        Task<ApiResponse<ParkingResponseDTO>> GetByIdAsync(Guid id, decimal? latitude = null, decimal? longitude = null);
        Task<ApiResponse<LocationDetailsDTO>> GetDetailsAsync(Guid ownerId, Guid id);
        Task<ApiResponse<ParkingResponseDTO>> CreateAsync(Guid ownerId, CreateParkingDTO dto);
        Task<ApiResponse<ParkingResponseDTO>> UpdateAsync(Guid ownerId, Guid id, UpdateParkingDTO dto);
        Task<ApiResponse> DeleteAsync(Guid ownerId, Guid id);
        Task<ApiResponse<SearchParkingPageDTO>> SearchAsync(SearchParkingQuery query);
        Task<ApiResponse<NearbyParkingPageDTO>> GetNearbyAsync(NearbyParkingQuery query);
        Task<ApiResponse<RecommendParkingPageDTO>> GetRecommendationsAsync(Guid userId, RecommendParkingQuery query);
    }
}